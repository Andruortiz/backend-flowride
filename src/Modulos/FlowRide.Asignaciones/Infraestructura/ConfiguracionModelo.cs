using FlowRide.Asignaciones.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Asignaciones.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    private const string EstadosActivosAsignacion = "estado IN ('PendienteAceptacion','Aceptada','EnCurso')";

    public IReadOnlyList<Type> Entidades { get; } = [typeof(Asignacion), typeof(ConfiguracionAsignacion)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<Asignacion>(e =>
            {
                e.ToTable("asignaciones");
                e.Property(x => x.Motivo).HasMaxLength(500);
                e.Property(x => x.Explicacion).ComoJson();
                e.HasIndex(x => x.SolicitudId);
                e.HasIndex(x => new { x.ConductorId, x.Estado });
    
                // Invariante: una solicitud tiene a lo sumo una asignación viva. Cubre la carrera entre dos reasignaciones.
                e.HasIndex(x => x.SolicitudId, "ux_asignaciones_activa_por_solicitud").IsUnique().HasFilter(EstadosActivosAsignacion);
            });

        mb.Entity<ConfiguracionAsignacion>(e =>
            {
                e.ToTable("configuraciones_asignacion");
                e.HasIndex(x => x.Version).IsUnique();
                e.Property(x => x.Reglas).ComoJson();
            });
    }
}
