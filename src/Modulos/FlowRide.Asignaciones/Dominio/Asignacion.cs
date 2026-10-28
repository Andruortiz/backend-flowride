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

public enum EstadoAsignacion
{
    PendienteAceptacion = 1,
    Aceptada = 2,
    EnCurso = 3,
    Finalizada = 4,
    Rechazada = 5,
    Reasignada = 6,
    Cancelada = 7,
}

/// <summary>Conductor y vehículo comprometidos con una solicitud. Solo puede haber una asignación activa por solicitud.</summary>
public sealed class Asignacion : RaizAgregado
{
    private Asignacion(
        Guid id,
        Guid solicitudId,
        Guid conductorId,
        Guid vehiculoId,
        int versionConfiguracion,
        DecisionAsignacion? explicacion,
        bool esManual,
        string? motivo,
        DateTimeOffset creadaEn)
        : base(id)
    {
        SolicitudId = solicitudId;
        ConductorId = conductorId;
        VehiculoId = vehiculoId;
        VersionConfiguracion = versionConfiguracion;
        Explicacion = explicacion;
        EsManual = esManual;
        Motivo = motivo;
        CreadaEn = creadaEn;
        Estado = EstadoAsignacion.PendienteAceptacion;
    }

    public Guid SolicitudId { get; }

    public Guid ConductorId { get; }

    public Guid VehiculoId { get; }

    public EstadoAsignacion Estado { get; private set; }

    /// <summary>Versión de la configuración de reglas con la que se decidió.</summary>
    public int VersionConfiguracion { get; }

    /// <summary>Por qué se eligió este conductor (RF-11); nulo en una reasignación manual.</summary>
    public DecisionAsignacion? Explicacion { get; }

    public bool EsManual { get; }

    public string? Motivo { get; }

    public DateTimeOffset CreadaEn { get; }

    public DateTimeOffset? RespondidaEn { get; private set; }

    public bool EsActiva => Estado is EstadoAsignacion.PendienteAceptacion or EstadoAsignacion.Aceptada or EstadoAsignacion.EnCurso;

    public static Asignacion Crear(
        Guid solicitudId,
        Guid conductorId,
        Guid vehiculoId,
        int versionConfiguracion,
        DecisionAsignacion? explicacion,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        var asignacion = new Asignacion(
            UuidHelper.GetOrGenerate(id), solicitudId, conductorId, vehiculoId, versionConfiguracion, explicacion, false, null, ahora);
        asignacion.Registrar(new AsignacionCreada(asignacion.Id, solicitudId, conductorId, vehiculoId, false, ahora));

        return asignacion;
    }

    /// <summary>RF-12: la crea el operador logístico y exige un motivo para la auditoría.</summary>
    public static Asignacion CrearManual(
        Guid solicitudId,
        Guid conductorId,
        Guid vehiculoId,
        int versionConfiguracion,
        string motivo,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        if (TextHelper.IsEmptyWithTrim(motivo))
        {
            throw new DomainException("Indica el motivo de la reasignación manual.");
        }

        var asignacion = new Asignacion(
            UuidHelper.GetOrGenerate(id), solicitudId, conductorId, vehiculoId, versionConfiguracion, null, true, motivo.Trim(), ahora);
        asignacion.Registrar(new AsignacionCreada(asignacion.Id, solicitudId, conductorId, vehiculoId, true, ahora));

        return asignacion;
    }

    public void Aceptar(DateTimeOffset ahora)
    {
        Exigir(EstadoAsignacion.PendienteAceptacion, "aceptar");
        Estado = EstadoAsignacion.Aceptada;
        RespondidaEn = ahora;
        Registrar(new AsignacionAceptada(Id, SolicitudId, ConductorId, ahora));
    }

    public void Rechazar(DateTimeOffset ahora)
    {
        Exigir(EstadoAsignacion.PendienteAceptacion, "rechazar");
        Estado = EstadoAsignacion.Rechazada;
        RespondidaEn = ahora;
        Registrar(new AsignacionRechazada(Id, SolicitudId, ConductorId, ahora));
    }

    public void Iniciar()
    {
        Exigir(EstadoAsignacion.Aceptada, "iniciar");
        Estado = EstadoAsignacion.EnCurso;
    }

    public void Finalizar()
    {
        Exigir(EstadoAsignacion.EnCurso, "finalizar");
        Estado = EstadoAsignacion.Finalizada;
    }

    /// <summary>La reemplaza <paramref name="nueva"/>; la anterior queda en el histórico (RF-12).</summary>
    public void Reasignar(Asignacion nueva, string motivo, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(nueva);

        if (Estado is not (EstadoAsignacion.PendienteAceptacion or EstadoAsignacion.Aceptada))
        {
            throw new DomainException($"Una asignación {Estado} no se puede reasignar.");
        }

        Estado = EstadoAsignacion.Reasignada;
        RespondidaEn = ahora;
        Registrar(new AsignacionReasignada(Id, nueva.Id, SolicitudId, ConductorId, nueva.ConductorId, motivo, ahora));
    }

    /// <summary>La solicitud se canceló antes de empezar.</summary>
    public void Cancelar(DateTimeOffset ahora)
    {
        if (Estado is not (EstadoAsignacion.PendienteAceptacion or EstadoAsignacion.Aceptada))
        {
            throw new DomainException($"Una asignación {Estado} no se puede cancelar.");
        }

        Estado = EstadoAsignacion.Cancelada;
        RespondidaEn = ahora;
    }

    private void Exigir(EstadoAsignacion esperado, string accion)
    {
        if (Estado != esperado)
        {
            throw new DomainException($"Una asignación {Estado} no se puede {accion}.");
        }
    }
}
