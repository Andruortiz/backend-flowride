namespace FlowRide.Nucleo.Persistencia;

/// <summary>Confirma en una sola transacción los cambios de todos los agregados y los eventos del outbox (ADR-0007).</summary>
public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
