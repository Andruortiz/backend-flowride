using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;

namespace FlowRide.Plataforma.Dominio;

public enum ResultadoAuditoria
{
    Aceptada = 1,
    Rechazada = 2,
}

/// <summary>Evidencia de una acción sensible: quién, qué, cuándo y con qué resultado (RF-52). Se conserva 5 años.</summary>
public sealed class RegistroAuditoria : Entidad
{
    private RegistroAuditoria(
        Guid id,
        Guid? actorId,
        string actorNombre,
        string accion,
        string recurso,
        ResultadoAuditoria resultado,
        string? motivo,
        string? antes,
        string? despues,
        DateTimeOffset instante)
        : base(id)
    {
        ActorId = actorId;
        ActorNombre = actorNombre;
        Accion = accion;
        Recurso = recurso;
        Resultado = resultado;
        Motivo = motivo;
        Antes = antes;
        Despues = despues;
        Instante = instante;
    }

    public Guid? ActorId { get; }

    public string ActorNombre { get; }

    public string Accion { get; }

    public string Recurso { get; }

    public ResultadoAuditoria Resultado { get; }

    public string? Motivo { get; }

    public string? Antes { get; }

    public string? Despues { get; }

    public DateTimeOffset Instante { get; }

    public static RegistroAuditoria Registrar(
        Guid? actorId,
        string actorNombre,
        string accion,
        string recurso,
        ResultadoAuditoria resultado,
        DateTimeOffset instante,
        string? motivo = null,
        string? antes = null,
        string? despues = null,
        Guid? id = null)
    {
        if (TextHelper.IsEmptyWithTrim(accion) || TextHelper.IsEmptyWithTrim(recurso))
        {
            throw new DomainException("La auditoría requiere la acción y el recurso.");
        }

        return new RegistroAuditoria(
            UuidHelper.GetOrGenerate(id),
            actorId,
            TextHelper.IsEmptyWithTrim(actorNombre) ? "Sistema" : actorNombre.Trim(),
            accion.Trim(),
            recurso.Trim(),
            resultado,
            motivo,
            antes,
            despues,
            instante);
    }
}
