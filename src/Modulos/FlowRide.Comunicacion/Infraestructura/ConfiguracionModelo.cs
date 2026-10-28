using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Infraestructura.Repositorios;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Comunicacion.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(ConversacionBot), typeof(AlertaOperativa)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<ConversacionBot>(e =>
            {
                e.ToTable("conversaciones");
                e.HasIndex(x => x.UsuarioId).IsUnique();
                e.Property(x => x.Origen).HasMaxLength(300);
                e.Property(x => x.Destino).HasMaxLength(300);
            });

        mb.Entity<AlertaOperativa>(e =>
            {
                e.ToTable("alertas");
                e.Property(x => x.Tipo).HasMaxLength(60);
                e.Property(x => x.Titulo).HasMaxLength(200);
                e.Property(x => x.Detalle).HasMaxLength(1000);
                e.Property(x => x.Referencia).HasMaxLength(200);
                e.Property(x => x.ClaveDeduplicacion).HasMaxLength(300);
                e.HasIndex(x => x.ClaveDeduplicacion).IsUnique();
                e.HasIndex(x => x.LeidaEn);
            });

        mb.Entity<MensajeEntranteRegistrado>(e =>
            {
                e.ToTable("mensajes_entrantes");
                e.HasKey(x => x.ExternalMessageId);
                e.Property(x => x.ExternalMessageId).HasMaxLength(200);
            });

        mb.Entity<NotificacionEnviadaRegistro>(e =>
            {
                e.ToTable("notificaciones_enviadas");
                e.HasKey(x => new { x.EventoId, x.DestinatarioId, x.Canal });
                e.Property(x => x.Canal).HasMaxLength(20);
                e.Property(x => x.Clave).HasMaxLength(100);
            });
    }
}
