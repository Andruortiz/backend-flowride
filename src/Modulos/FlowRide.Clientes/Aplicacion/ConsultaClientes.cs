using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;

namespace FlowRide.Clientes.Aplicacion;

internal sealed class ConsultaClientes(IClienteEmpresarialRepository clientes, IItinerarioRepository itinerarios) : IConsultaClientes
{
    public Task<ClienteEmpresarial?> ObtenerClienteAsync(Guid id, CancellationToken ct = default) =>
        clientes.ObtenerAsync(id, ct);

    public Task<Itinerario?> ObtenerItinerarioAsync(Guid id, CancellationToken ct = default) =>
        itinerarios.ObtenerAsync(id, ct);

    public Task<bool> ExisteItinerarioDePasajeroAsync(Guid pasajeroId, string codigoServicio, DateOnly fecha, CancellationToken ct = default) =>
        itinerarios.ExisteDePasajeroAsync(pasajeroId, codigoServicio, fecha, ct);
}
