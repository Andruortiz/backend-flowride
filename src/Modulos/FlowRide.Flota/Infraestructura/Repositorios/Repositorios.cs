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
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Flota.Infraestructura.Controladores;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Flota.Infraestructura.Repositorios;

internal sealed class ConductorRepository(FlowRideDbContext db) : IConductorRepository
{
    public Task<Conductor?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Conductor>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Conductor?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        db.Set<Conductor>().FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public async Task<IReadOnlyDictionary<Guid, Conductor>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();

        return await db.Set<Conductor>().Where(c => lista.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
    }

    public async Task<IReadOnlyList<Conductor>> ListarAsync(bool soloActivos, CancellationToken ct = default) =>
        await db.Set<Conductor>().Where(c => !soloActivos || c.Activo).OrderBy(c => c.Id).ToListAsync(ct);

    public async Task AgregarAsync(Conductor conductor, CancellationToken ct = default) => await db.Set<Conductor>().AddAsync(conductor, ct);
}

internal sealed class VehiculoRepository(FlowRideDbContext db) : IVehiculoRepository
{
    public Task<Vehiculo?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Vehiculo>().FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Vehiculo?> ObtenerPorPlacaAsync(string placa, CancellationToken ct = default) =>
        db.Set<Vehiculo>().FirstOrDefaultAsync(v => v.Placa == placa, ct);

    public async Task<IReadOnlyDictionary<Guid, Vehiculo>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();

        return await db.Set<Vehiculo>().Where(v => lista.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);
    }

    public async Task<IReadOnlyList<Vehiculo>> ListarAsync(CancellationToken ct = default) =>
        await db.Set<Vehiculo>().OrderBy(v => v.Placa).ToListAsync(ct);

    public async Task AgregarAsync(Vehiculo vehiculo, CancellationToken ct = default) => await db.Set<Vehiculo>().AddAsync(vehiculo, ct);
}
