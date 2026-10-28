using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Viajes.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Asignaciones.Dominio;

public sealed record MotivoDescarte(Guid ConductorId, string Regla, string Motivo);

public sealed record DesgloseRegla(string Regla, double Peso, double Puntos);

public sealed record PuntajeCandidato(Guid ConductorId, double Total, IReadOnlyList<DesgloseRegla> Desglose);

/// <summary>Explicación completa de una decisión: con ella se reproduce y se audita (ADR-0009).</summary>
public sealed record DecisionAsignacion(
    int VersionConfiguracion,
    IReadOnlyList<string> ReglasAplicadas,
    IReadOnlyList<MotivoDescarte> Descartados,
    IReadOnlyList<PuntajeCandidato> Ranking,
    Guid? ConductorElegido,
    string CriterioDesempate);

public sealed record ResultadoAsignacion(CandidatoAsignacion? Ganador, DecisionAsignacion Decision)
{
    public bool Exitoso => Ganador is not null;
}

/// <summary>
/// Orquesta las reglas configuradas: elimina, prioriza, puntúa y desempata. Misma entrada, mismo resultado.
/// </summary>
public sealed class AssignmentEngine
{
    private readonly IReadOnlyDictionary<string, IAssignmentRule> _catalogo;

    public AssignmentEngine(IEnumerable<IAssignmentRule>? catalogo = null)
    {
        _catalogo = (catalogo ?? CatalogoReglas.Todas).ToDictionary(r => r.Id);
    }

    public ResultadoAsignacion Asignar(
        ContextoAsignacion contexto,
        IReadOnlyList<CandidatoAsignacion> candidatos,
        ConfiguracionAsignacion configuracion)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(candidatos);
        ArgumentNullException.ThrowIfNull(configuracion);

        ValidadorConfiguracion.Validar(configuracion.Reglas, [.. _catalogo.Values]);

        var activas = configuracion.Reglas
            .Where(c => c.Activa)
            .OrderBy(c => c.Orden)
            .Select(c => (Config: c, Regla: _catalogo[c.ReglaId]))
            .ToList();

        var eliminatorias = activas.Where(a => a.Regla.Tipo == TipoRegla.Eliminatoria).ToList();
        var puntuables = activas.Where(a => a.Regla.Tipo != TipoRegla.Eliminatoria).ToList();

        var descartados = new List<MotivoDescarte>();
        var ranking = new List<PuntajeCandidato>();
        var vivos = new Dictionary<Guid, CandidatoAsignacion>();

        foreach (var candidato in candidatos.OrderBy(c => c.Conductor.Id))
        {
            var descarte = eliminatorias
                .Select(e => (e.Regla, Evaluacion: e.Regla.Evaluar(contexto, candidato)))
                .FirstOrDefault(r => r.Evaluacion.Descarta);

            if (descarte.Regla is not null)
            {
                descartados.Add(new MotivoDescarte(candidato.Conductor.Id, descarte.Regla.Nombre, descarte.Evaluacion.Motivo ?? string.Empty));
                continue;
            }

            var desglose = puntuables
                .Select(p => new DesgloseRegla(p.Regla.Nombre, p.Config.Peso, Math.Round(p.Regla.Evaluar(contexto, candidato).Puntos * p.Config.Peso, 4)))
                .ToList();

            vivos[candidato.Conductor.Id] = candidato;
            ranking.Add(new PuntajeCandidato(candidato.Conductor.Id, Math.Round(desglose.Sum(d => d.Puntos), 4), desglose));
        }

        // Desempate estable: a igual puntaje, el identificador del conductor. Nunca aleatorio.
        var ordenado = ranking.OrderByDescending(r => r.Total).ThenBy(r => r.ConductorId).ToList();
        var hayEmpate = ordenado.Count > 1 && ordenado[0].Total == ordenado[1].Total;
        var elegido = ordenado.Count > 0 ? ordenado[0].ConductorId : (Guid?)null;

        var decision = new DecisionAsignacion(
            configuracion.Version,
            [.. activas.Select(a => a.Regla.Nombre)],
            descartados,
            ordenado,
            elegido,
            hayEmpate ? "Empate resuelto por identificador estable del conductor" : "Sin empate");

        return new ResultadoAsignacion(elegido is { } id ? vivos[id] : null, decision);
    }
}
