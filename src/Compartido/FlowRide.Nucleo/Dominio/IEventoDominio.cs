namespace FlowRide.Nucleo.Dominio;

public interface IEventoDominio
{
    Guid EventoId { get; }

    DateTimeOffset OcurrioEn { get; }
}

public abstract record EventoDominio(DateTimeOffset OcurrioEn) : IEventoDominio
{
    public Guid EventoId { get; init; } = Guid.CreateVersion7();
}
