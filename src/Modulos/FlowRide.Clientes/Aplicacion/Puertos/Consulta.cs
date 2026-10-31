using FlowRide.Clientes.Dominio;

namespace FlowRide.Clientes.Aplicacion.Puertos;

public interface IConsultaClientes
{
    Task<ClienteEmpresarial?> ObtenerClienteAsync(Guid id, CancellationToken ct = default);
    Task<Itinerario?> ObtenerItinerarioAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteItinerarioDePasajeroAsync(Guid pasajeroId, string codigoServicio, DateOnly fecha, CancellationToken ct = default);
}
