using FlowRide.Solicitudes.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Solicitudes.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(SolicitudTransporte)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<SolicitudTransporte>(e =>
            {
                e.ToTable("solicitudes");
                e.Property(x => x.Origen).HasMaxLength(300);
                e.Property(x => x.Destino).HasMaxLength(300);
                e.Property(x => x.MotivoCancelacion).HasMaxLength(500);
                e.HasIndex(x => new { x.Estado, x.FechaHoraProgramada });
                e.HasIndex(x => x.PasajeroId);
            });
    }
}
