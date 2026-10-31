using Microsoft.EntityFrameworkCore;

namespace FlowRide.Persistencia;

public interface IConfiguradorModelo
{
    IReadOnlyList<Type> Entidades { get; }

    void Configurar(ModelBuilder modelo);
}
