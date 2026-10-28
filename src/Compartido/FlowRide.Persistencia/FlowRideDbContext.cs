using System.Text.Json;
using FlowRide.Nucleo.Dominio;
using FlowRide.Persistencia.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Persistencia;

/// <summary>
/// Un solo contexto y una sola transacción para todos los módulos (ADR-0007). Las tablas las aportan los módulos
/// por medio de <see cref="IConfiguradorModelo"/>; aquí solo viven las convenciones y el outbox.
/// </summary>
public sealed class FlowRideDbContext(
    DbContextOptions<FlowRideDbContext> opciones,
    TimeProvider reloj,
    IEnumerable<IConfiguradorModelo> configuradores) : DbContext(opciones)
{
    public DbSet<OutboxMensaje> Outbox => Set<OutboxMensaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ops");

        var lista = configuradores.ToList();

        // Primero todas las propiedades de solo lectura: las configuraciones siguientes ya las pueden indexar.
        foreach (var configurador in lista)
        {
            ConvencionesModelo.MapearPropiedadesDeSoloLectura(modelBuilder, configurador.Entidades);
        }

        foreach (var configurador in lista)
        {
            configurador.Configurar(modelBuilder);
        }

        modelBuilder.Entity<OutboxMensaje>(e =>
        {
            e.ToTable("outbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(200);
            e.Property(x => x.Carga).HasColumnType("jsonb");
            e.Property(x => x.UltimoError).HasMaxLength(2000);
            e.HasIndex(x => x.DisponibleDesde).HasFilter("procesado_en IS NULL");
        });

        ConvencionesModelo.Aplicar(modelBuilder);
    }

    /// <summary>Los eventos de los agregados modificados se guardan en el outbox dentro de la misma transacción.</summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        CapturarEventos();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void CapturarEventos()
    {
        var ahora = reloj.GetUtcNow();
        var raices = ChangeTracker.Entries<RaizAgregado>().Select(e => e.Entity).Where(r => r.EventosDominio.Count > 0).ToList();

        foreach (var raiz in raices)
        {
            foreach (var evento in raiz.EventosDominio)
            {
                Outbox.Add(new OutboxMensaje
                {
                    Id = evento.EventoId,
                    Tipo = evento.GetType().FullName!,
                    Carga = JsonSerializer.Serialize(evento, evento.GetType(), ColumnasJson.Opciones),
                    OcurrioEn = evento.OcurrioEn,
                    DisponibleDesde = ahora,
                });
            }

            raiz.LimpiarEventos();
        }
    }
}
