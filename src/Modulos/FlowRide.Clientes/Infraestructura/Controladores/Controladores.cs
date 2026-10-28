using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
using FlowRide.Clientes.Infraestructura.Repositorios;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Clientes.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/clientes")]
public sealed class ClientesController(IServicioClientes servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Administracion)]
    public async Task<ActionResult<ClienteEmpresarialResponse>> Crear(CrearClienteEmpresarialRequest request, CancellationToken ct)
    {
        var creado = await servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Gestion)]
    public async Task<ClienteEmpresarialResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<ClienteEmpresarialResponse>> Listar(CancellationToken ct) => await servicio.ListarAsync(ct);

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.Administracion)]
    public async Task<ClienteEmpresarialResponse> Actualizar(Guid id, CrearClienteEmpresarialRequest request, CancellationToken ct) =>
        await servicio.ActualizarAsync(id, request, ct);

    [HttpPost("{id:guid}/activar")]
    [Authorize(Politicas.Administracion)]
    public async Task<ClienteEmpresarialResponse> Activar(Guid id, CancellationToken ct) => await servicio.CambiarEstadoAsync(id, true, ct);

    [HttpPost("{id:guid}/inactivar")]
    [Authorize(Politicas.Administracion)]
    public async Task<ClienteEmpresarialResponse> Inactivar(Guid id, CancellationToken ct) => await servicio.CambiarEstadoAsync(id, false, ct);
}

[ApiController]
[Route("api/v1/itinerarios")]
public sealed class ItinerariosController(IServicioClientes servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Administracion)]
    public async Task<ItinerarioResponse> Crear(CrearItinerarioRequest request, CancellationToken ct) =>
        await servicio.CrearItinerarioAsync(request, ct);

    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<ItinerarioResponse>> Listar(Guid? clienteEmpresarialId, CancellationToken ct) =>
        await servicio.ListarItinerariosAsync(clienteEmpresarialId, ct);
}
