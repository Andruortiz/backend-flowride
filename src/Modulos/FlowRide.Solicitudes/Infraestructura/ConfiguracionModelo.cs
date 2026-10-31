using FlowRide.Solicitudes.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Solicitudes.Infraestructura;

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
