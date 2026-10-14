namespace FlowRide.Dominio.Compartido;

public abstract class Entidad : IEquatable<Entidad>
{
    protected Entidad(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador no puede ser vacío.", nameof(id));
        }

        Id = id;
    }

    public Guid Id { get; }

    public bool Equals(Entidad? otro) =>
        otro is not null && GetType() == otro.GetType() && Id == otro.Id;

    public override bool Equals(object? obj) => Equals(obj as Entidad);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}