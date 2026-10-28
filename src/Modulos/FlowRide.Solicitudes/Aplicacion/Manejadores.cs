using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Solicitudes.Aplicacion;

/// <summary>El viaje empezó: el servicio de la solicitud pasa a EnCurso.</summary>
internal sealed class AlIniciarViaje(ISolicitudRepository solicitudes, IUnitOfWork unidad) : IManejadorEvento<ViajeIniciado>
{
    public async Task ManejarAsync(ViajeIniciado evento, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(evento.SolicitudId, ct);

        if (solicitud is not { Estado: EstadoSolicitud.Asignada })
        {
            return;
        }

        solicitud.IniciarServicio();
        await unidad.GuardarCambiosAsync(ct);
    }
}

/// <summary>El viaje terminó: la solicitud queda Finalizada.</summary>
internal sealed class AlFinalizarViaje(ISolicitudRepository solicitudes, IUnitOfWork unidad) : IManejadorEvento<ViajeFinalizado>
{
    public async Task ManejarAsync(ViajeFinalizado evento, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(evento.SolicitudId, ct);

        if (solicitud is null or { Estado: EstadoSolicitud.Finalizada })
        {
            return;
        }

        if (solicitud.Estado == EstadoSolicitud.Asignada)
        {
            // El evento de inicio aún no se procesó: se reintenta para respetar el orden.
            throw new InvalidOperationException("La solicitud todavía no registra el inicio del viaje.");
        }

        solicitud.Finalizar();
        await unidad.GuardarCambiosAsync(ct);
    }
}
