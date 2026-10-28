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
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Plataforma.Infraestructura.Controladores;

namespace FlowRide.Plataforma.Infraestructura.Repositorios;

internal sealed class ParametroRepository(FlowRideDbContext db) : IParametroRepository
{
    public async Task<IReadOnlyList<ParametroOperador>> ListarAsync(CancellationToken ct = default) =>
        await db.Set<ParametroOperador>().OrderBy(p => p.Clave).ToListAsync(ct);

    public Task<ParametroOperador?> ObtenerAsync(string clave, CancellationToken ct = default) =>
        db.Set<ParametroOperador>().FirstOrDefaultAsync(p => p.Clave == clave, ct);

    public async Task AgregarAsync(ParametroOperador parametro, CancellationToken ct = default) => await db.Set<ParametroOperador>().AddAsync(parametro, ct);
}

internal sealed class OperadorRepository(FlowRideDbContext db) : IOperadorRepository
{
    public Task<Operador?> ObtenerAsync(CancellationToken ct = default) => db.Set<Operador>().FirstOrDefaultAsync(ct);

    public async Task AgregarAsync(Operador operador, CancellationToken ct = default) => await db.Set<Operador>().AddAsync(operador, ct);
}

internal sealed class AuditoriaRepository(FlowRideDbContext db) : IAuditoriaRepository
{
    public async Task AgregarAsync(RegistroAuditoria registro, CancellationToken ct = default) => await db.Set<RegistroAuditoria>().AddAsync(registro, ct);

    public async Task<IReadOnlyList<RegistroAuditoria>> ConsultarAsync(
        string? accion, string? recurso, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<RegistroAuditoria>()
            .AsNoTracking()
            .Where(r => (accion == null || r.Accion == accion)
                && (recurso == null || r.Recurso.StartsWith(recurso))
                && (desde == null || r.Instante >= desde)
                && (hasta == null || r.Instante < hasta))
            .OrderByDescending(r => r.Instante).ThenBy(r => r.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);
}
