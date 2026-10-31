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

    public int VersionConfiguracion { get; }

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
