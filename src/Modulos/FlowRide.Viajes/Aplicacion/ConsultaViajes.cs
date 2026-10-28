using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;

namespace FlowRide.Viajes.Aplicacion;

internal sealed class ConsultaViajes(IViajeRepository viajes) : IConsultaViajes
{
    public Task<Viaje?> ObtenerVigenteDeConductorAsync(Guid conductorId, CancellationToken ct = default) =>
        viajes.ObtenerVigenteDeConductorAsync(conductorId, ct);

    public Task<Viaje?> ObtenerUltimoFinalizadoDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default) =>
        viajes.ObtenerUltimoFinalizadoDePasajeroAsync(pasajeroId, ct);
}
