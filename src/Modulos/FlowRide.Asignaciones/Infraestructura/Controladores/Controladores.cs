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
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Asignaciones.Infraestructura.Repositorios;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/asignaciones")]
public sealed class AsignacionesController(IServicioAsignaciones servicio) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Gestion)]
    public async Task<AsignacionResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.GestionOConductor)]
    public async Task<IReadOnlyList<AsignacionResponse>> Listar(
        Guid? conductorId, EstadoAsignacion? estado, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ListarAsync(conductorId, estado, pagina, tamano, ct);

    [HttpPost("{id:guid}/aceptar")]
    [Authorize(Politicas.Conductor)]
    public async Task<AsignacionResponse> Aceptar(Guid id, CancellationToken ct) => await servicio.AceptarAsync(id, ct);

    [HttpPost("{id:guid}/rechazar")]
    [Authorize(Politicas.Conductor)]
    public async Task<AsignacionResponse> Rechazar(Guid id, CancellationToken ct) => await servicio.RechazarAsync(id, ct);

    [HttpGet("configuracion")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConfiguracionAsignacionResponse> ObtenerConfiguracion(CancellationToken ct) => await servicio.ObtenerConfiguracionAsync(ct);

    [HttpPut("configuracion")]
    [Authorize(Politicas.Administracion)]
    public async Task<ConfiguracionAsignacionResponse> ActualizarConfiguracion(ActualizarConfiguracionAsignacionRequest request, CancellationToken ct) =>
        await servicio.ActualizarConfiguracionAsync(request, ct);
}
