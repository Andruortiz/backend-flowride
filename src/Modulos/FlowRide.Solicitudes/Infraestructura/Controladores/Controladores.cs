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
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Infraestructura.Repositorios;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Solicitudes.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/solicitudes")]
public sealed class SolicitudesController(IServicioSolicitudes servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Autenticado)]
    public async Task<ActionResult<SolicitudResponse>> Registrar(CrearSolicitudRequest request, CancellationToken ct)
    {
        var creada = await servicio.RegistrarAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Autenticado)]
    public async Task<SolicitudResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.Autenticado)]
    public async Task<IReadOnlyList<SolicitudResponse>> Listar(
        EstadoSolicitud? estado, Guid? pasajeroId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ListarAsync(estado, pasajeroId, desde, hasta, pagina, tamano, ct);

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.Autenticado)]
    public async Task<SolicitudResponse> Modificar(Guid id, ModificarSolicitudRequest request, CancellationToken ct) =>
        await servicio.ModificarAsync(id, request, ct);

    [HttpPost("{id:guid}/cancelar")]
    [Authorize(Politicas.Autenticado)]
    public async Task<SolicitudResponse> Cancelar(Guid id, CancelarSolicitudRequest request, CancellationToken ct) =>
        await servicio.CancelarAsync(id, request, ct);
}
