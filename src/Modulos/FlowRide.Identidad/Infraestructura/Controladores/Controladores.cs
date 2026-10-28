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
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Infraestructura.Repositorios;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Identidad.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/usuarios")]
public sealed class UsuariosController(IServicioUsuarios servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Administracion)]
    public async Task<ActionResult<UsuarioResponse>> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var creado = await servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Gestion)]
    public async Task<UsuarioResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<UsuarioResponse>> Listar(
        RolUsuario? rol, bool? activo, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ListarAsync(rol, activo, pagina, tamano, ct);

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.Administracion)]
    public async Task<UsuarioResponse> Actualizar(Guid id, ActualizarUsuarioRequest request, CancellationToken ct) =>
        await servicio.ActualizarAsync(id, request, ct);

    [HttpPut("{id:guid}/rol")]
    [Authorize(Politicas.Administracion)]
    public async Task<UsuarioResponse> CambiarRol(Guid id, CambiarRolRequest request, CancellationToken ct) =>
        await servicio.CambiarRolAsync(id, request, ct);

    [HttpPost("{id:guid}/desactivar")]
    [Authorize(Politicas.Administracion)]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await servicio.DesactivarAsync(id, ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    [Authorize(Politicas.Administracion)]
    public async Task<IActionResult> Activar(Guid id, CancellationToken ct)
    {
        await servicio.ActivarAsync(id, ct);

        return NoContent();
    }
}
