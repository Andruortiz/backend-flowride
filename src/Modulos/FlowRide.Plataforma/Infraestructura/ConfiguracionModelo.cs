using FlowRide.Plataforma.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Plataforma.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(Operador), typeof(ParametroOperador), typeof(RegistroAuditoria)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<Operador>(e =>
            {
                e.ToTable("operador");
                e.Property(x => x.RazonSocial).HasMaxLength(200);
                e.Property(x => x.Nit).HasMaxLength(30);
                e.Property(x => x.CorreoContacto).HasMaxLength(200);
                e.Property(x => x.ZonaHoraria).HasMaxLength(64);
            });

        mb.Entity<ParametroOperador>(e =>
            {
                e.ToTable("parametros");
                e.Property(x => x.Clave).HasMaxLength(100);
                e.HasIndex(x => x.Clave).IsUnique();
            });

        mb.Entity<RegistroAuditoria>(e =>
            {
                e.ToTable("registros", "audit");
                e.Property(x => x.ActorNombre).HasMaxLength(150);
                e.Property(x => x.Accion).HasMaxLength(100);
                e.Property(x => x.Recurso).HasMaxLength(200);
                e.Property(x => x.Motivo).HasMaxLength(1000);
                e.HasIndex(x => x.Instante);
                e.HasIndex(x => x.Accion);
            });
    }
}
