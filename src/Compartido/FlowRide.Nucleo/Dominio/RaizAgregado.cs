namespace FlowRide.Nucleo.Dominio;

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
