using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;

namespace FlowRide.Viajes.Aplicacion;

internal sealed class AlCancelarSolicitud(IViajeRepository viajes, IUnitOfWork unidad, TimeProvider reloj) : IManejadorEvento<SolicitudCancelada>
{
    public async Task ManejarAsync(SolicitudCancelada evento, CancellationToken ct = default)
    {
        if (await viajes.ObtenerPorSolicitudAsync(evento.SolicitudId, ct) is not { Estado: EstadoViaje.Programado } viaje)
        {
            return;
        }

        viaje.Cancelar(reloj.GetUtcNow());
        await unidad.GuardarCambiosAsync(ct);
    }
}
