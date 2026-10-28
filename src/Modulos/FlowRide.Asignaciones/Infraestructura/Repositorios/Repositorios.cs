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
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Asignaciones.Infraestructura.Controladores;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Infraestructura.Repositorios;

internal sealed class AsignacionRepository(FlowRideDbContext db) : IAsignacionRepository
{
    private static readonly EstadoAsignacion[] Vivas = [EstadoAsignacion.PendienteAceptacion, EstadoAsignacion.Aceptada, EstadoAsignacion.EnCurso];

    public Task<Asignacion?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Asignacion>().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Asignacion?> ObtenerActivaPorSolicitudAsync(Guid solicitudId, CancellationToken ct = default) =>
        db.Set<Asignacion>().FirstOrDefaultAsync(a => a.SolicitudId == solicitudId && Vivas.Contains(a.Estado), ct);

    public Task<Asignacion?> ObtenerPendienteDeConductorAsync(Guid conductorId, CancellationToken ct = default) =>
        db.Set<Asignacion>()
            .Where(a => a.ConductorId == conductorId && a.Estado == EstadoAsignacion.PendienteAceptacion)
            .OrderBy(a => a.CreadaEn).ThenBy(a => a.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlySet<Guid>> ConductoresDescartadosAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var ids = await db.Set<Asignacion>()
            .Where(a => a.SolicitudId == solicitudId && (a.Estado == EstadoAsignacion.Rechazada || a.Estado == EstadoAsignacion.Reasignada))
            .Select(a => a.ConductorId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ContarActivasPorConductorAsync(IEnumerable<Guid> conductorIds, CancellationToken ct = default)
    {
        var lista = conductorIds.ToList();

        return await db.Set<Asignacion>()
            .Where(a => lista.Contains(a.ConductorId) && Vivas.Contains(a.Estado))
            .GroupBy(a => a.ConductorId)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> ContarRecientesPorConductorAsync(IEnumerable<Guid> conductorIds, DateTimeOffset desde, CancellationToken ct = default)
    {
        var lista = conductorIds.ToList();

        return await db.Set<Asignacion>()
            .Where(a => lista.Contains(a.ConductorId) && a.CreadaEn >= desde)
            .GroupBy(a => a.ConductorId)
            .Select(g => new { Id = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Total, ct);
    }

    public async Task<IReadOnlyList<Asignacion>> ListarAsync(Guid? conductorId, EstadoAsignacion? estado, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<Asignacion>()
            .Where(a => (conductorId == null || a.ConductorId == conductorId) && (estado == null || a.Estado == estado))
            .OrderByDescending(a => a.CreadaEn).ThenBy(a => a.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);

    public async Task AgregarAsync(Asignacion asignacion, CancellationToken ct = default) => await db.Set<Asignacion>().AddAsync(asignacion, ct);
}

internal sealed class ConfiguracionAsignacionRepository(FlowRideDbContext db) : IConfiguracionAsignacionRepository
{
    public Task<ConfiguracionAsignacion?> ObtenerActivaAsync(CancellationToken ct = default) =>
        db.Set<ConfiguracionAsignacion>().OrderByDescending(c => c.Version).FirstOrDefaultAsync(ct);

    public async Task AgregarAsync(ConfiguracionAsignacion configuracion, CancellationToken ct = default) =>
        await db.Set<ConfiguracionAsignacion>().AddAsync(configuracion, ct);
}
