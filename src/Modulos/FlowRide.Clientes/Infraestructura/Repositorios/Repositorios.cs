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
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Clientes.Infraestructura.Controladores;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Clientes.Infraestructura.Repositorios;

internal sealed class ClienteEmpresarialRepository(FlowRideDbContext db) : IClienteEmpresarialRepository
{
    public Task<ClienteEmpresarial?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        db.Set<ClienteEmpresarial>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ClienteEmpresarial>> ListarAsync(CancellationToken ct = default) =>
        await db.Set<ClienteEmpresarial>().OrderBy(c => c.Nombre).ToListAsync(ct);

    public async Task AgregarAsync(ClienteEmpresarial cliente, CancellationToken ct = default) => await db.Set<ClienteEmpresarial>().AddAsync(cliente, ct);
}

internal sealed class ItinerarioRepository(FlowRideDbContext db) : IItinerarioRepository
{
    public Task<Itinerario?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<Itinerario>().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Itinerario>> ListarAsync(Guid? clienteEmpresarialId, CancellationToken ct = default) =>
        await db.Set<Itinerario>()
            .Where(i => clienteEmpresarialId == null || i.ClienteEmpresarialId == clienteEmpresarialId)
            .OrderBy(i => i.Fecha).ThenBy(i => i.HoraSalida).ThenBy(i => i.Id)
            .ToListAsync(ct);

    public async Task AgregarAsync(Itinerario itinerario, CancellationToken ct = default) => await db.Set<Itinerario>().AddAsync(itinerario, ct);
}
