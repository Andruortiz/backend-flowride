using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Clientes.Aplicacion.Puertos;

public interface IServicioClientes
{
    Task<ClienteEmpresarialResponse> CrearAsync(CrearClienteEmpresarialRequest request, CancellationToken ct = default);

    Task<ClienteEmpresarialResponse> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteEmpresarialResponse>> ListarAsync(CancellationToken ct = default);

    Task<ClienteEmpresarialResponse> ActualizarAsync(Guid id, CrearClienteEmpresarialRequest request, CancellationToken ct = default);

    Task<ClienteEmpresarialResponse> CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct = default);

    Task<ItinerarioResponse> CrearItinerarioAsync(CrearItinerarioRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ItinerarioResponse>> ListarItinerariosAsync(Guid? clienteEmpresarialId, CancellationToken ct = default);
}
