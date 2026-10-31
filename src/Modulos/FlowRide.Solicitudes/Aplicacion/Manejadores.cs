using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Solicitudes.Aplicacion;

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
            throw new InvalidOperationException("La solicitud todavía no registra el inicio del viaje.");
        }

        solicitud.Finalizar();
        await unidad.GuardarCambiosAsync(ct);
    }
}
