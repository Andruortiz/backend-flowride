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
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Infraestructura.Controladores;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Solicitudes.Infraestructura.Repositorios;

internal sealed class SolicitudRepository(FlowRideDbContext db) : ISolicitudRepository
{
    public Task<SolicitudTransporte?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        db.Set<SolicitudTransporte>().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<SolicitudTransporte?> ObtenerUltimaDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default) =>
        db.Set<SolicitudTransporte>().Where(s => s.PasajeroId == pasajeroId).OrderByDescending(s => s.CreadaEn).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SolicitudTransporte>> ListarAsync(
        EstadoSolicitud? estado, Guid? pasajeroId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<SolicitudTransporte>()
            .Where(s => (estado == null || s.Estado == estado)
                && (pasajeroId == null || s.PasajeroId == pasajeroId)
                && (desde == null || s.FechaHoraProgramada >= desde)
                && (hasta == null || s.FechaHoraProgramada < hasta))
            .OrderByDescending(s => s.FechaHoraProgramada).ThenBy(s => s.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudTransporte>> ListarParaRecordatorioAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        await db.Set<SolicitudTransporte>()
            .Where(s => s.Estado == EstadoSolicitud.Asignada && !s.RecordatorioEnviado && s.FechaHoraProgramada >= desde && s.FechaHoraProgramada <= hasta)
            .OrderBy(s => s.FechaHoraProgramada).ThenBy(s => s.Id)
            .Take(500)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SolicitudTransporte>> ListarAbiertasAntesDeAsync(DateTimeOffset limite, CancellationToken ct = default) =>
        await db.Set<SolicitudTransporte>()
            .AsNoTracking()
            .Where(s => (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.EnRevision) && s.CreadaEn <= limite)
            .OrderBy(s => s.CreadaEn).ThenBy(s => s.Id)
            .Take(500)
            .ToListAsync(ct);

    public async Task AgregarAsync(SolicitudTransporte solicitud, CancellationToken ct = default) => await db.Set<SolicitudTransporte>().AddAsync(solicitud, ct);
}
