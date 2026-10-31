using System.Globalization;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Aplicacion;

internal sealed class ComposicionMensajes(
    IConsultaSolicitudes solicitudes,
    IConsultaIdentidad identidad,
    IConsultaFlota flota,
    IServicioOperador operador)
{
    public async Task<Dictionary<string, string>> DeSolicitudAsync(Guid solicitudId, CancellationToken ct)
    {
        var solicitud = await solicitudes.ObtenerAsync(solicitudId, ct);
        var zona = await operador.ZonaAsync(ct);
        var variables = new Dictionary<string, string>();

        if (solicitud is null)
        {
            return variables;
        }

        variables["origen"] = solicitud.Origen;
        variables["destino"] = solicitud.Destino;
        variables["fecha"] = Fecha(solicitud.FechaHoraProgramada, zona);

        if (await identidad.ObtenerPasajeroAsync(solicitud.PasajeroId, ct) is { } pasajero
            && await identidad.ObtenerUsuarioAsync(pasajero.UsuarioId, ct) is { } usuario)
        {
            variables["pasajero"] = usuario.Nombre;
        }

        return variables;
    }

    public async Task AgregarConductorAsync(Dictionary<string, string> variables, Guid conductorId, Guid? vehiculoId, CancellationToken ct)
    {
        if (await flota.ObtenerConductorAsync(conductorId, ct) is { } conductor
            && await identidad.ObtenerUsuarioAsync(conductor.UsuarioId, ct) is { } usuario)
        {
            variables["conductor"] = usuario.Nombre;
        }

        if (vehiculoId is { } id && await flota.ObtenerVehiculoAsync(id, ct) is { } vehiculo)
        {
            variables["placa"] = vehiculo.Placa;
        }
    }

    public static string Fecha(DateTimeOffset instante, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTime(instante, zona).ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-CO"));
}

internal sealed class AlRegistrarSolicitud(IServicioNotificaciones notificaciones, ComposicionMensajes mensajes) : IManejadorEvento<SolicitudRegistrada>
{
    public async Task ManejarAsync(SolicitudRegistrada evento, CancellationToken ct = default)
    {
        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAPasajeroAsync(evento.PasajeroId, evento.EventoId, TipoNotificacion.SolicitudRegistrada, "solicitud.registrada", variables, ct);
    }
}

internal sealed class AlCrearAsignacion(IServicioNotificaciones notificaciones, ComposicionMensajes mensajes) : IManejadorEvento<AsignacionCreada>
{
    public async Task ManejarAsync(AsignacionCreada evento, CancellationToken ct = default)
    {
        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAConductorAsync(evento.ConductorId, evento.EventoId, TipoNotificacion.AsignacionConductor, "asignacion.conductor", variables, ct);
    }
}

internal sealed class AlAceptarAsignacion(IServicioNotificaciones notificaciones, IConsultaSolicitudes solicitudes, IConsultaAsignaciones asignaciones, ComposicionMensajes mensajes)
    : IManejadorEvento<AsignacionAceptada>
{
    public async Task ManejarAsync(AsignacionAceptada evento, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(evento.SolicitudId, ct);
        var asignacion = await asignaciones.ObtenerAsync(evento.AsignacionId, ct);

        if (solicitud is null || asignacion is null)
        {
            return;
        }

        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await mensajes.AgregarConductorAsync(variables, evento.ConductorId, asignacion.VehiculoId, ct);
        await notificaciones.EnviarAPasajeroAsync(solicitud.PasajeroId, evento.EventoId, TipoNotificacion.AsignacionPasajero, "asignacion.pasajero", variables, ct);
    }
}

internal sealed class AlReasignar(IServicioNotificaciones notificaciones, ComposicionMensajes mensajes) : IManejadorEvento<AsignacionReasignada>
{
    public async Task ManejarAsync(AsignacionReasignada evento, CancellationToken ct = default)
    {
        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAConductorAsync(evento.ConductorAnteriorId, evento.EventoId, TipoNotificacion.ReasignacionConductorAnterior, "reasignacion.conductor_anterior", variables, ct);
    }
}

internal sealed class AlQuedarSinRecurso(IServicioNotificaciones notificaciones, IServicioAlertas alertas, ComposicionMensajes mensajes)
    : IManejadorEvento<SinRecursoDisponible>
{
    public async Task ManejarAsync(SinRecursoDisponible evento, CancellationToken ct = default)
    {
        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAPasajeroAsync(evento.PasajeroId, evento.EventoId, TipoNotificacion.SinConductorPasajero, "sin_conductor.pasajero", variables, ct);
        await alertas.CrearAsync(
            "sin_conductor", SeveridadAlerta.Critica, "Solicitud sin conductor", evento.Motivo, $"solicitud/{evento.SolicitudId}", $"sin_conductor:{evento.SolicitudId}", ct);
    }
}

internal sealed class AlCancelarSolicitud(IServicioNotificaciones notificaciones, IConsultaAsignaciones asignaciones, ComposicionMensajes mensajes)
    : IManejadorEvento<SolicitudCancelada>
{
    public async Task ManejarAsync(SolicitudCancelada evento, CancellationToken ct = default)
    {
        if (evento.AsignacionId is not { } asignacionId || await asignaciones.ObtenerAsync(asignacionId, ct) is not { } asignacion)
        {
            return;
        }

        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAConductorAsync(asignacion.ConductorId, evento.EventoId, TipoNotificacion.SolicitudCanceladaConductor, "solicitud_cancelada.conductor", variables, ct);
    }
}

internal sealed class AlIniciarViaje(IServicioNotificaciones notificaciones, ComposicionMensajes mensajes) : IManejadorEvento<ViajeIniciado>
{
    public async Task ManejarAsync(ViajeIniciado evento, CancellationToken ct = default)
    {
        var variables = new Dictionary<string, string>();
        await mensajes.AgregarConductorAsync(variables, evento.ConductorId, null, ct);
        await notificaciones.EnviarAPasajeroAsync(evento.PasajeroId, evento.EventoId, TipoNotificacion.ViajeIniciado, "viaje_iniciado.pasajero", variables, ct);
    }
}

internal sealed class AlFinalizarViaje(IServicioNotificaciones notificaciones) : IManejadorEvento<ViajeFinalizado>
{
    public Task ManejarAsync(ViajeFinalizado evento, CancellationToken ct = default) =>
        notificaciones.EnviarAPasajeroAsync(evento.PasajeroId, evento.EventoId, TipoNotificacion.ViajeFinalizado, "viaje_finalizado.pasajero", new Dictionary<string, string>(), ct);
}

internal sealed class AlDebeRecordar(IServicioNotificaciones notificaciones, ComposicionMensajes mensajes) : IManejadorEvento<RecordatorioViajeDebido>
{
    public async Task ManejarAsync(RecordatorioViajeDebido evento, CancellationToken ct = default)
    {
        var variables = await mensajes.DeSolicitudAsync(evento.SolicitudId, ct);
        await notificaciones.EnviarAPasajeroAsync(evento.PasajeroId, evento.EventoId, TipoNotificacion.Recordatorio, "recordatorio.viaje", variables, ct);
    }
}

internal sealed class AlDesviarseLaEta(IServicioNotificaciones notificaciones, IServicioAlertas alertas) : IManejadorEvento<DesviacionEtaDetectada>
{
    public async Task ManejarAsync(DesviacionEtaDetectada evento, CancellationToken ct = default)
    {
        var minutos = Math.Round(evento.DesvioMinutos).ToString("0", CultureInfo.InvariantCulture);
        await notificaciones.EnviarAPasajeroAsync(
            evento.PasajeroId, evento.EventoId, TipoNotificacion.DesviacionEtaPasajero, "eta_desviacion.pasajero", new Dictionary<string, string> { ["minutos"] = minutos }, ct);
        await alertas.CrearAsync(
            "desviacion_eta", SeveridadAlerta.Atencion, "Viaje con retraso", $"La llegada se estima {minutos} min después de lo previsto.", $"viaje/{evento.ViajeId}", $"eta:{evento.EventoId}", ct);
    }
}

internal sealed class AlReportarIncidente(IServicioAlertas alertas) : IManejadorEvento<IncidenteReportado>
{
    public Task ManejarAsync(IncidenteReportado evento, CancellationToken ct = default) =>
        alertas.CrearAsync("incidente", SeveridadAlerta.Critica, "Incidente en viaje", evento.Descripcion, $"viaje/{evento.ViajeId}", $"incidente:{evento.EventoId}", ct);
}
