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
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Infraestructura.Repositorios;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Viajes.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/viajes")]
public sealed class ViajesController(IServicioViajes servicio) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Politicas.GestionOConductor)]
    public async Task<ViajeResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.GestionOConductor)]
    public async Task<IReadOnlyList<ViajeResponse>> Listar(
        EstadoViaje? estado, Guid? conductorId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ListarAsync(estado, conductorId, desde, hasta, pagina, tamano, ct);

    [HttpPost("{id:guid}/iniciar")]
    [Authorize(Politicas.Conductor)]
    public async Task<ViajeResponse> Iniciar(Guid id, CancellationToken ct) => await servicio.IniciarAsync(id, ct);

    [HttpPost("{id:guid}/finalizar")]
    [Authorize(Politicas.Conductor)]
    public async Task<ViajeResponse> Finalizar(Guid id, CancellationToken ct) => await servicio.FinalizarAsync(id, ct);

    [HttpPost("{id:guid}/incidentes")]
    [Authorize(Politicas.Conductor)]
    public async Task<ViajeResponse> ReportarIncidente(Guid id, ReportarIncidenteRequest request, CancellationToken ct) =>
        await servicio.ReportarIncidenteAsync(id, request, ct);

    [HttpPost("{id:guid}/calificacion")]
    [Authorize(Politicas.Pasajero)]
    public async Task<CalificacionResponse> Calificar(Guid id, CalificarViajeRequest request, CancellationToken ct) =>
        await servicio.CalificarAsync(id, request, ct);
}

[ApiController]
[Route("api/v1/ubicaciones")]
public sealed class UbicacionesController(IServicioSeguimiento servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Gestion)]
    public async Task<UbicacionResponse> Reportar(ReportarUbicacionRequest request, CancellationToken ct) =>
        await servicio.ReportarAsync(request, ct);
}
