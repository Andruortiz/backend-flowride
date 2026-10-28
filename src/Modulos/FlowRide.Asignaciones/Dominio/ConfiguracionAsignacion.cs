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

public sealed record ConfiguracionRegla(string ReglaId, bool Activa, int Orden, double Peso);

/// <summary>Reglas activas, su orden y sus pesos para un operador. Cada cambio crea una versión nueva, para reproducir decisiones.</summary>
public sealed class ConfiguracionAsignacion : Entidad
{
    private ConfiguracionAsignacion(Guid id, int version, IReadOnlyList<ConfiguracionRegla> reglas, Guid? creadaPor, DateTimeOffset creadaEn)
        : base(id)
    {
        Version = version;
        Reglas = reglas;
        CreadaPor = creadaPor;
        CreadaEn = creadaEn;
    }

    public int Version { get; }

    public IReadOnlyList<ConfiguracionRegla> Reglas { get; }

    public Guid? CreadaPor { get; }

    public DateTimeOffset CreadaEn { get; }

    public static ConfiguracionAsignacion PorDefecto(DateTimeOffset ahora) =>
        Crear(
            1,
            [
                new("disponibilidad", true, 1, 1),
                new("turno", true, 2, 1),
                new("regulatoria", true, 3, 1),
                new("capacidad", true, 4, 1),
                new("urgente", true, 5, 0.5),
                new("balance_carga", true, 6, 0.6),
                new("recientes", true, 7, 0.4),
            ],
            null,
            ahora);

    public static ConfiguracionAsignacion Crear(
        int version,
        IEnumerable<ConfiguracionRegla> reglas,
        Guid? creadaPor,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(reglas);

        if (version < 1)
        {
            throw new DomainException("La versión de la configuración debe ser positiva.");
        }

        var lista = reglas.OrderBy(r => r.Orden).ToList();
        ValidadorConfiguracion.Validar(lista, CatalogoReglas.Todas);

        return new ConfiguracionAsignacion(UuidHelper.GetOrGenerate(id), version, lista, creadaPor, ahora);
    }
}

/// <summary>Rechaza configuraciones inválidas antes de activarlas (ADR-0009).</summary>
public static class ValidadorConfiguracion
{
    public static void Validar(IReadOnlyList<ConfiguracionRegla> reglas, IReadOnlyList<IAssignmentRule> catalogo)
    {
        var errores = new List<string>();
        var porId = catalogo.ToDictionary(r => r.Id);

        foreach (var duplicada in reglas.GroupBy(r => r.ReglaId).Where(g => g.Count() > 1))
        {
            errores.Add($"La regla '{duplicada.Key}' está repetida.");
        }

        foreach (var regla in reglas)
        {
            if (!porId.TryGetValue(regla.ReglaId, out var definicion))
            {
                errores.Add($"La regla '{regla.ReglaId}' no existe.");
                continue;
            }

            if (regla.Peso is < 0 or > 1)
            {
                errores.Add($"El peso de '{definicion.Nombre}' debe estar entre 0 y 1.");
            }

            if (!regla.Activa && definicion.Obligatoria)
            {
                errores.Add($"La regla '{definicion.Nombre}' es obligatoria y no se puede desactivar.");
            }
        }

        foreach (var obligatoria in catalogo.Where(r => r.Obligatoria))
        {
            if (!reglas.Any(r => r.ReglaId == obligatoria.Id))
            {
                errores.Add($"Falta la regla obligatoria '{obligatoria.Nombre}'.");
            }
        }

        if (reglas.GroupBy(r => r.Orden).Any(g => g.Count() > 1))
        {
            errores.Add("Dos reglas no pueden tener el mismo orden.");
        }

        var puntuaActiva = reglas.Any(r => r.Activa && porId.TryGetValue(r.ReglaId, out var d) && d.Tipo == TipoRegla.Puntuacion);
        if (!puntuaActiva)
        {
            errores.Add("Debe haber al menos una regla de puntuación activa.");
        }

        if (errores.Count > 0)
        {
            throw new DomainException(string.Join(" ", errores));
        }
    }
}
