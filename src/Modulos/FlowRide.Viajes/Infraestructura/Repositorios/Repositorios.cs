using Microsoft.EntityFrameworkCore;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Persistencia;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Infraestructura.Controladores;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Viajes.Infraestructura.Repositorios;

internal sealed class ViajeRepository(FlowRideDbContext db) : IViajeRepository
{
    public Task<Viaje?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Viaje>().FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Viaje?> ObtenerPorSolicitudAsync(Guid solicitudId, CancellationToken ct = default) =>
        db.Set<Viaje>().Where(v => v.SolicitudId == solicitudId && v.Estado != EstadoViaje.Cancelado)
            .OrderByDescending(v => v.ProgramadoPara).FirstOrDefaultAsync(ct);

    public Task<Viaje?> ObtenerPorAsignacionAsync(Guid asignacionId, CancellationToken ct = default) =>
        db.Set<Viaje>().FirstOrDefaultAsync(v => v.AsignacionId == asignacionId, ct);

    public Task<Viaje?> ObtenerEnCursoDeVehiculoAsync(Guid vehiculoId, CancellationToken ct = default) =>
        db.Set<Viaje>().FirstOrDefaultAsync(v => v.VehiculoId == vehiculoId && v.Estado == EstadoViaje.EnCurso, ct);

    public Task<Viaje?> ObtenerVigenteDeConductorAsync(Guid conductorId, CancellationToken ct = default) =>
        db.Set<Viaje>()
            .Where(v => v.ConductorId == conductorId && (v.Estado == EstadoViaje.EnCurso || v.Estado == EstadoViaje.Programado))
            .OrderBy(v => v.Estado == EstadoViaje.EnCurso ? 0 : 1).ThenBy(v => v.ProgramadoPara).ThenBy(v => v.Id)
            .FirstOrDefaultAsync(ct);

    public Task<Viaje?> ObtenerUltimoFinalizadoDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default) =>
        db.Set<Viaje>().Where(v => v.PasajeroId == pasajeroId && v.Estado == EstadoViaje.Finalizado)
            .OrderByDescending(v => v.FinalizadoEn).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Viaje>> ListarAsync(
        EstadoViaje? estado, Guid? conductorId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<Viaje>()
            .Where(v => (estado == null || v.Estado == estado)
                && (conductorId == null || v.ConductorId == conductorId)
                && (desde == null || v.ProgramadoPara >= desde)
                && (hasta == null || v.ProgramadoPara < hasta))
            .OrderByDescending(v => v.ProgramadoPara).ThenBy(v => v.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);

    public async Task AgregarAsync(Viaje viaje, CancellationToken ct = default) => await db.Set<Viaje>().AddAsync(viaje, ct);
}

internal sealed class CalificacionRepository(FlowRideDbContext db) : ICalificacionRepository
{
    public Task<bool> ExistePorViajeAsync(Guid viajeId, CancellationToken ct = default) => db.Set<Calificacion>().AnyAsync(c => c.ViajeId == viajeId, ct);

    public async Task<IReadOnlyDictionary<Guid, double>> PromedioPorConductorAsync(IEnumerable<Guid> conductorIds, CancellationToken ct = default)
    {
        var lista = conductorIds.ToList();

        return await db.Set<Calificacion>()
            .Where(c => lista.Contains(c.ConductorId))
            .GroupBy(c => c.ConductorId)
            .Select(g => new { Id = g.Key, Promedio = g.Average(c => (double)c.Puntaje) })
            .ToDictionaryAsync(x => x.Id, x => x.Promedio, ct);
    }

    public async Task AgregarAsync(Calificacion calificacion, CancellationToken ct = default) => await db.Set<Calificacion>().AddAsync(calificacion, ct);
}

internal sealed class UbicacionRepository(FlowRideDbContext db) : IUbicacionRepository
{
    public Task<bool> ExisteAsync(Guid vehiculoId, DateTimeOffset timestampOrigen, string fuente, CancellationToken ct = default) =>
        db.Set<RegistroUbicacion>().AnyAsync(u => u.VehiculoId == vehiculoId && u.TimestampOrigen == timestampOrigen && u.Fuente == fuente, ct);

    public async Task<IReadOnlyList<RegistroUbicacion>> ObtenerRecientesAsync(Guid vehiculoId, DateTimeOffset desde, int maximo, CancellationToken ct = default)
    {
        var ultimas = await db.Set<RegistroUbicacion>()
            .AsNoTracking()
            .Where(u => u.VehiculoId == vehiculoId && u.TimestampOrigen >= desde)
            .OrderByDescending(u => u.TimestampOrigen)
            .Take(maximo)
            .ToListAsync(ct);

        var pendientes = db.ChangeTracker.Entries<RegistroUbicacion>()
            .Where(e => e.State == EntityState.Added && e.Entity.VehiculoId == vehiculoId && e.Entity.TimestampOrigen >= desde)
            .Select(e => e.Entity);

        return [.. ultimas.Concat(pendientes).DistinctBy(u => u.Id).OrderBy(u => u.TimestampOrigen).TakeLast(maximo)];
    }

    public async Task AgregarAsync(RegistroUbicacion registro, CancellationToken ct = default) => await db.Set<RegistroUbicacion>().AddAsync(registro, ct);
}
