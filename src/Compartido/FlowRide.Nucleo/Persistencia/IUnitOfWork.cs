namespace FlowRide.Nucleo.Persistencia;

public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken ct = default);
}
