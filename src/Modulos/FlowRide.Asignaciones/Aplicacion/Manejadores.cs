using System.Globalization;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Aplicacion;

/// <summary>Un rechazo devuelve la solicitud a la cola: se busca otro conductor.</summary>
internal sealed class AlRechazarAsignacion(IServicioAsignaciones asignaciones) : IManejadorEvento<AsignacionRechazada>
{
    public Task ManejarAsync(AsignacionRechazada evento, CancellationToken ct = default) =>
        asignaciones.AsignarAutomaticoAsync(evento.SolicitudId, ct);
}

/// <summary>Una solicitud nueva entra a la cola: se ejecuta el motor de asignación.</summary>
internal sealed class AlRegistrarSolicitud(IServicioAsignaciones asignaciones) : IManejadorEvento<SolicitudRegistrada>
{
    public Task ManejarAsync(SolicitudRegistrada evento, CancellationToken ct = default) =>
        asignaciones.AsignarAutomaticoAsync(evento.SolicitudId, ct);
}

/// <summary>Si cancelan la solicitud, la oferta vigente se cancela.</summary>
internal sealed class AlCancelarSolicitud(IAsignacionRepository asignaciones, IUnitOfWork unidad, TimeProvider reloj) : IManejadorEvento<SolicitudCancelada>
{
    public async Task ManejarAsync(SolicitudCancelada evento, CancellationToken ct = default)
    {
        if (await asignaciones.ObtenerActivaPorSolicitudAsync(evento.SolicitudId, ct) is not { Estado: EstadoAsignacion.PendienteAceptacion or EstadoAsignacion.Aceptada } asignacion)
        {
            return;
        }

        asignacion.Cancelar(reloj.GetUtcNow());
        await unidad.GuardarCambiosAsync(ct);
    }
}

/// <summary>El viaje empezó: la asignación pasa a EnCurso.</summary>
internal sealed class AlIniciarViaje(IAsignacionRepository asignaciones, IUnitOfWork unidad) : IManejadorEvento<ViajeIniciado>
{
    public async Task ManejarAsync(ViajeIniciado evento, CancellationToken ct = default)
    {
        if (await asignaciones.ObtenerActivaPorSolicitudAsync(evento.SolicitudId, ct) is not { Estado: EstadoAsignacion.Aceptada } asignacion)
        {
            return;
        }

        asignacion.Iniciar();
        await unidad.GuardarCambiosAsync(ct);
    }
}

/// <summary>El viaje terminó: la asignación queda Finalizada.</summary>
internal sealed class AlFinalizarViaje(IAsignacionRepository asignaciones, IUnitOfWork unidad) : IManejadorEvento<ViajeFinalizado>
{
    public async Task ManejarAsync(ViajeFinalizado evento, CancellationToken ct = default)
    {
        var asignacion = await asignaciones.ObtenerActivaPorSolicitudAsync(evento.SolicitudId, ct);

        switch (asignacion?.Estado)
        {
            case EstadoAsignacion.EnCurso:
                asignacion.Finalizar();
                await unidad.GuardarCambiosAsync(ct);
                break;

            case EstadoAsignacion.Aceptada:
                // El evento de inicio aún no se procesó: se reintenta para respetar el orden.
                throw new InvalidOperationException("La asignación todavía no registra el inicio del viaje.");
        }
    }
}
