using FlowRide.Identidad.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Identidad.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(Usuario), typeof(Pasajero)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
            {
                e.ToTable("usuarios");
                e.Property(x => x.Nombre).HasMaxLength(150);
                e.Property(x => x.Correo).HasMaxLength(200);
                e.Property(x => x.Telefono).HasMaxLength(20);
                e.Property(x => x.CodigoInterno).HasMaxLength(50);
                e.Property(x => x.IdentidadExterna).HasMaxLength(200);
                e.HasIndex(x => x.CodigoInterno).IsUnique();
                e.HasIndex(x => x.Correo).IsUnique().HasFilter("correo IS NOT NULL");
                e.HasIndex(x => x.Telefono).IsUnique().HasFilter("telefono IS NOT NULL");
                e.HasIndex(x => x.IdentidadExterna).IsUnique().HasFilter("identidad_externa IS NOT NULL");
            });

        mb.Entity<Pasajero>(e =>
            {
                e.ToTable("pasajeros");
                e.HasIndex(x => x.UsuarioId).IsUnique();
                e.HasIndex(x => x.ClienteEmpresarialId);
            });
    }
}
