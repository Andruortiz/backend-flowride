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
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Infraestructura.Controladores;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Identidad.Infraestructura.Repositorios;

internal sealed class UsuarioRepository(FlowRideDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        db.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorTelefonoAsync(string telefono, CancellationToken ct = default)
    {
        // WhatsApp entrega el número sin '+'; en la base se guarda en formato internacional.
        var limpio = telefono.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var normalizado = limpio.StartsWith('+') ? limpio : "+" + limpio;

        return db.Set<Usuario>().FirstOrDefaultAsync(u => u.Telefono == normalizado, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();

        return await db.Set<Usuario>().Where(u => lista.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        db.Set<Usuario>().AnyAsync(u => u.CodigoInterno == codigo && u.Id != excluirId, ct);

    public Task<bool> ExisteCorreoAsync(string correo, Guid? excluirId, CancellationToken ct = default) =>
        db.Set<Usuario>().AnyAsync(u => u.Correo == correo && u.Id != excluirId, ct);

    public Task<bool> ExisteTelefonoAsync(string telefono, Guid? excluirId, CancellationToken ct = default) =>
        db.Set<Usuario>().AnyAsync(u => u.Telefono == telefono && u.Id != excluirId, ct);

    public async Task<IReadOnlyList<Usuario>> ListarAsync(RolUsuario? rol, bool? activo, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<Usuario>()
            .Where(u => (rol == null || u.Rol == rol) && (activo == null || u.Activo == activo))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);

    public async Task AgregarAsync(Usuario usuario, CancellationToken ct = default) => await db.Set<Usuario>().AddAsync(usuario, ct);
}

internal sealed class PasajeroRepository(FlowRideDbContext db) : IPasajeroRepository
{
    public Task<Pasajero?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Pasajero>().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Pasajero?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        db.Set<Pasajero>().FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);

    public async Task<IReadOnlyDictionary<Guid, Pasajero>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();

        return await db.Set<Pasajero>().Where(p => lista.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
    }

    public async Task AgregarAsync(Pasajero pasajero, CancellationToken ct = default) => await db.Set<Pasajero>().AddAsync(pasajero, ct);
}
