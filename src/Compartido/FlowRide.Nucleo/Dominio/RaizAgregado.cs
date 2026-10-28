namespace FlowRide.Nucleo.Dominio;

/// <summary>Entidad que acumula eventos de dominio; la infraestructura los publica por outbox al confirmar.</summary>
public abstract class RaizAgregado : Entidad
{
    private readonly List<IEventoDominio> _eventos = [];

    protected RaizAgregado(Guid id)
        : base(id)
    {
    }

    public IReadOnlyList<IEventoDominio> EventosDominio => _eventos;

    public void LimpiarEventos() => _eventos.Clear();

    protected void Registrar(IEventoDominio evento) => _eventos.Add(evento);
}
