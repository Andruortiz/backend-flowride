using FlowRide.Nucleo.Dominio;

namespace FlowRide.Nucleo.Eventos;

public interface IManejadorEvento<in TEvento>
    where TEvento : IEventoDominio
{
    Task ManejarAsync(TEvento evento, CancellationToken ct = default);
}
