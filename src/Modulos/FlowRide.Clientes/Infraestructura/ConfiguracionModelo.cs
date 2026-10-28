using FlowRide.Clientes.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Clientes.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(ClienteEmpresarial), typeof(Itinerario)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<ClienteEmpresarial>(e =>
            {
                e.ToTable("clientes_empresariales");
                e.Property(x => x.Nombre).HasMaxLength(200);
                e.Property(x => x.Nit).HasMaxLength(30);
            });

        mb.Entity<Itinerario>(e =>
            {
                e.ToTable("itinerarios");
                e.Property(x => x.CodigoServicio).HasMaxLength(50);
                e.Property(x => x.Origen).HasMaxLength(300);
                e.Property(x => x.Destino).HasMaxLength(300);
                e.HasIndex(x => new { x.ClienteEmpresarialId, x.Fecha });
            });
    }
}
