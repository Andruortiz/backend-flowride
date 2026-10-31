using FlowRide.Viajes.Dominio;

namespace FlowRide.Viajes.Aplicacion.Puertos;

public interface IConsultaViajes
{
    Task<Viaje?> ObtenerVigenteDeConductorAsync(Guid conductorId, CancellationToken ct = default);
    Task<Viaje?> ObtenerUltimoFinalizadoDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);
}
