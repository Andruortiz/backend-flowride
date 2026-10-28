using FlowRide.Flota.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Flota.Infraestructura;

/// <summary>Tablas e índices del módulo. Los compone el DbContext único (ADR-0004).</summary>
internal sealed class ConfiguracionModelo : IConfiguradorModelo
{
    public IReadOnlyList<Type> Entidades { get; } = [typeof(Vehiculo), typeof(Conductor)];

    public void Configurar(ModelBuilder mb)
    {
        mb.Entity<Vehiculo>(e =>
            {
                e.ToTable("vehiculos");
                e.Property(x => x.Placa).HasMaxLength(10);
                e.HasIndex(x => x.Placa).IsUnique();
                e.Ignore(x => x.Documentos);
                e.Property<List<DocumentoVigencia>>("_documentos").HasColumnName("documentos").ComoDocumentosJson();
            });

        mb.Entity<Conductor>(e =>
            {
                e.ToTable("conductores");
                e.HasIndex(x => x.UsuarioId).IsUnique();
                e.Property(x => x.Licencia).HasColumnName("licencia").ComoDocumentoJson();
                e.Property(x => x.MotivoNoDisponible).HasMaxLength(300);
                e.Ignore(x => x.Turnos);
                e.Property<List<Turno>>("_turnos").HasColumnName("turnos").ComoTurnosJson();
            });
    }
}
