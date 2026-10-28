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

internal interface IClienteEmpresarialRepository
{
    Task<ClienteEmpresarial?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteEmpresarial>> ListarAsync(CancellationToken ct = default);

    Task AgregarAsync(ClienteEmpresarial cliente, CancellationToken ct = default);
}

internal interface IItinerarioRepository
{
    Task<Itinerario?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Itinerario>> ListarAsync(Guid? clienteEmpresarialId, CancellationToken ct = default);

    Task AgregarAsync(Itinerario itinerario, CancellationToken ct = default);
}
