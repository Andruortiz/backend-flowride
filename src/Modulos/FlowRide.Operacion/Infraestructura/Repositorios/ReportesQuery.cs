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
using FlowRide.Operacion.Aplicacion;
using FlowRide.Operacion.Aplicacion.Dtos;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Operacion.Infraestructura;
using FlowRide.Operacion.Infraestructura.Controladores;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;

namespace FlowRide.Operacion.Infraestructura.Repositorios;

internal sealed class ReportesQuery(FlowRideDbContext db) : IReportesQuery
{
    public async Task<IReadOnlyList<ViajeRealizadoReporte>> ViajesRealizadosAsync(
        DateTimeOffset desde, DateTimeOffset hasta, Guid? clienteEmpresarialId, CancellationToken ct = default) =>
        await (from v in db.Set<Viaje>().AsNoTracking()
               join p in db.Set<Pasajero>().AsNoTracking() on v.PasajeroId equals p.Id
               where v.Estado == EstadoViaje.Finalizado && v.FinalizadoEn >= desde && v.FinalizadoEn < hasta
                   && (clienteEmpresarialId == null || p.ClienteEmpresarialId == clienteEmpresarialId)
               orderby v.FinalizadoEn, v.Id
               select new ViajeRealizadoReporte(v.Id, v.ConductorId, v.VehiculoId, v.PasajeroId, p.ClienteEmpresarialId, v.ProgramadoPara, v.IniciadoEn, v.FinalizadoEn))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudesPorEstadoReporte>> SolicitudesPorEstadoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        await db.Set<SolicitudTransporte>().AsNoTracking()
            .Where(s => s.FechaHoraProgramada >= desde && s.FechaHoraProgramada < hasta)
            .GroupBy(s => s.Estado)
            .Select(g => new SolicitudesPorEstadoReporte(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ConductorAsignadoReporte>> ConductoresAsignadosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default)
    {
        var asignaciones = await db.Set<Asignacion>().AsNoTracking()
            .Where(a => a.CreadaEn >= desde && a.CreadaEn < hasta)
            .GroupBy(a => a.ConductorId)
            .Select(g => new
            {
                ConductorId = g.Key,
                Total = g.Count(),
                Rechazos = g.Count(a => a.Estado == EstadoAsignacion.Rechazada),
                Finalizadas = g.Count(a => a.Estado == EstadoAsignacion.Finalizada),
            })
            .ToListAsync(ct);

        var ids = asignaciones.Select(a => a.ConductorId).ToList();
        var nombres = await (from c in db.Set<Conductor>().AsNoTracking()
                             join u in db.Set<Usuario>().AsNoTracking() on c.UsuarioId equals u.Id
                             where ids.Contains(c.Id)
                             select new { c.Id, u.Nombre })
            .ToDictionaryAsync(x => x.Id, x => x.Nombre, ct);

        return
        [
            .. asignaciones
                .Select(a => new ConductorAsignadoReporte(a.ConductorId, nombres.GetValueOrDefault(a.ConductorId, string.Empty), a.Total, a.Rechazos, a.Finalizadas))
                .OrderByDescending(r => r.Asignaciones).ThenBy(r => r.ConductorId),
        ];
    }

    public async Task<IReadOnlyList<ClienteOperacionReporte>> OperacionPorClienteAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        await (from s in db.Set<SolicitudTransporte>().AsNoTracking()
               join p in db.Set<Pasajero>().AsNoTracking() on s.PasajeroId equals p.Id
               join c in db.Set<ClienteEmpresarial>().AsNoTracking() on p.ClienteEmpresarialId equals c.Id
               where s.FechaHoraProgramada >= desde && s.FechaHoraProgramada < hasta
               group s by new { c.Id, c.Nombre } into g
               orderby g.Key.Nombre
               select new ClienteOperacionReporte(
                   g.Key.Id,
                   g.Key.Nombre,
                   g.Count(),
                   g.Count(x => x.Estado == EstadoSolicitud.Finalizada),
                   g.Count(x => x.Estado == EstadoSolicitud.Cancelada)))
            .ToListAsync(ct);

    public async Task<IndicadoresResponse> IndicadoresAsync(DateTimeOffset desde, DateTimeOffset hasta, int umbralPuntualidadMinutos, CancellationToken ct = default)
    {
        var porEstado = await db.Set<SolicitudTransporte>().AsNoTracking()
            .Where(s => s.FechaHoraProgramada >= desde && s.FechaHoraProgramada < hasta)
            .GroupBy(s => s.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var asignaciones = await db.Set<Asignacion>().AsNoTracking()
            .Where(a => a.CreadaEn >= desde && a.CreadaEn < hasta)
            .GroupBy(a => a.EsManual)
            .Select(g => new { Manual = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        var salidas = await db.Set<Viaje>().AsNoTracking()
            .Where(v => v.Estado == EstadoViaje.Finalizado && v.FinalizadoEn >= desde && v.FinalizadoEn < hasta && v.IniciadoEn != null)
            .Select(v => new { v.ProgramadoPara, Inicio = v.IniciadoEn!.Value })
            .ToListAsync(ct);

        var calificacion = await db.Set<Calificacion>().AsNoTracking()
            .Where(c => c.CreadaEn >= desde && c.CreadaEn < hasta)
            .Select(c => (double)c.Puntaje)
            .ToListAsync(ct);

        int Contar(EstadoSolicitud estado) => porEstado.Where(p => p.Estado == estado).Sum(p => p.Total);

        var totalAsignaciones = asignaciones.Sum(a => a.Total);
        var automaticas = asignaciones.Where(a => !a.Manual).Sum(a => a.Total);
        var puntuales = salidas.Count(s => s.Inicio <= s.ProgramadoPara.AddMinutes(umbralPuntualidadMinutos));

        return new IndicadoresResponse(
            porEstado.Sum(p => p.Total),
            Contar(EstadoSolicitud.Finalizada),
            Contar(EstadoSolicitud.Cancelada),
            Contar(EstadoSolicitud.EnRevision),
            totalAsignaciones == 0 ? 0 : Math.Round(100.0 * automaticas / totalAsignaciones, 1),
            salidas.Count == 0 ? 0 : Math.Round(100.0 * puntuales / salidas.Count, 1),
            calificacion.Count == 0 ? 0 : Math.Round(calificacion.Average(), 2));
    }
}
