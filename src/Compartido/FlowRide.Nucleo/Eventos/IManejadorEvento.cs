using FlowRide.Nucleo.Dominio;

namespace FlowRide.Nucleo.Eventos;

/// <summary>Reacciona a un evento de dominio. Se ejecuta al menos una vez, así que debe ser idempotente (ADR-0007).</summary>
public interface IManejadorEvento<in TEvento>
    where TEvento : IEventoDominio
{
    Task ManejarAsync(TEvento evento, CancellationToken ct = default);
}
