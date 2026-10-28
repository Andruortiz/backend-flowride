using FlowRide.Viajes.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Viajes.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(Viaje), typeof(Calificacion), typeof(RegistroUbicacion)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<Viaje>(e =>
            {
                e.ToTable("viajes");
                e.HasIndex(x => x.AsignacionId).IsUnique();
                e.HasIndex(x => x.SolicitudId);
                e.HasIndex(x => new { x.ConductorId, x.Estado });
    
                // Invariante: un vehículo no hace dos viajes a la vez.
                e.HasIndex(x => x.VehiculoId, "ux_viajes_en_curso_por_vehiculo").IsUnique().HasFilter("estado = 'EnCurso'");
                e.Ignore(x => x.Hitos);
                e.Property<List<HitoViaje>>("_hitos").HasColumnName("hitos").ComoJson();
            });

        mb.Entity<Calificacion>(e =>
            {
                e.ToTable("calificaciones");
                e.Property(x => x.Comentario).HasMaxLength(Calificacion.LongitudMaximaComentario);
                e.HasIndex(x => x.ViajeId).IsUnique();
            });

        mb.Entity<RegistroUbicacion>(e =>
            {
                e.ToTable("ubicaciones");
                e.Property(x => x.Fuente).HasMaxLength(60);
                e.HasIndex(x => new { x.VehiculoId, x.TimestampOrigen }).IsDescending(false, true);
                e.HasIndex(x => new { x.VehiculoId, x.TimestampOrigen, x.Fuente }).IsUnique();
            });
    }
}
