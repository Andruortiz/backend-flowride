using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Clientes.Aplicacion;

internal sealed class ServicioClientes(
    IClienteEmpresarialRepository clientes,
    IItinerarioRepository itinerarios,
    IServicioAuditoria auditoria,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioClientes
{
    public async Task<ClienteEmpresarialResponse> CrearAsync(CrearClienteEmpresarialRequest request, CancellationToken ct = default)
    {
        var cliente = ClienteEmpresarial.Crear(request.Nombre, request.Nit);
        await clientes.AgregarAsync(cliente, ct);
        await auditoria.RegistrarAsync("cliente.crear", $"cliente/{cliente.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return cliente.A();
    }

    public async Task<ClienteEmpresarialResponse> ObtenerAsync(Guid id, CancellationToken ct = default) => (await Buscar(id, ct)).A();

    public async Task<IReadOnlyList<ClienteEmpresarialResponse>> ListarAsync(CancellationToken ct = default) =>
        [.. (await clientes.ListarAsync(ct)).Select(c => c.A())];

    public async Task<ClienteEmpresarialResponse> ActualizarAsync(Guid id, CrearClienteEmpresarialRequest request, CancellationToken ct = default)
    {
        var cliente = await Buscar(id, ct);
        cliente.Actualizar(request.Nombre, request.Nit);
        await auditoria.RegistrarAsync("cliente.actualizar", $"cliente/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return cliente.A();
    }

    public async Task<ClienteEmpresarialResponse> CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct = default)
    {
        var cliente = await Buscar(id, ct);

        if (activo)
        {
            cliente.Activar();
        }
        else
        {
            cliente.Inactivar();
        }

        await auditoria.RegistrarAsync(activo ? "cliente.activar" : "cliente.inactivar", $"cliente/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return cliente.A();
    }

    public async Task<ItinerarioResponse> CrearItinerarioAsync(CrearItinerarioRequest request, CancellationToken ct = default)
    {
        if (request.ClienteEmpresarialId is { } clienteId)
        {
            _ = await Buscar(clienteId, ct);
        }

        var itinerario = Itinerario.Crear(
            request.CodigoServicio, request.ClienteEmpresarialId, request.Origen, request.Destino, request.Fecha, request.HoraSalida, request.HoraLlegada, reloj.GetUtcNow());
        await itinerarios.AgregarAsync(itinerario, ct);
        await auditoria.RegistrarAsync("itinerario.crear", $"itinerario/{itinerario.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return itinerario.A();
    }

    public async Task<IReadOnlyList<ItinerarioResponse>> ListarItinerariosAsync(Guid? clienteEmpresarialId, CancellationToken ct = default) =>
        [.. (await itinerarios.ListarAsync(clienteEmpresarialId, ct)).Select(i => i.A())];

    private async Task<ClienteEmpresarial> Buscar(Guid id, CancellationToken ct) =>
        await clientes.ObtenerAsync(id, ct) ?? throw new NotFoundException("El cliente empresarial no existe.");
}
