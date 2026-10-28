using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Persistencia;
using FlowRide.Operacion.Aplicacion;
using FlowRide.Operacion.Aplicacion.Dtos;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Operacion.Infraestructura;
using FlowRide.Operacion.Infraestructura.Repositorios;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;

namespace FlowRide.Operacion.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/reportes")]
[Authorize(Politicas.Gestion)]
public sealed class ReportesController(IServicioReportes servicio) : ControllerBase
{
    [HttpGet("viajes")]
    public async Task<IReadOnlyList<ViajeRealizadoReporte>> Viajes(DateTimeOffset desde, DateTimeOffset hasta, Guid? clienteEmpresarialId, CancellationToken ct) =>
        await servicio.ViajesRealizadosAsync(desde, hasta, clienteEmpresarialId, ct);

    [HttpGet("solicitudes-por-estado")]
    public async Task<IReadOnlyList<SolicitudesPorEstadoReporte>> SolicitudesPorEstado(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct) =>
        await servicio.SolicitudesPorEstadoAsync(desde, hasta, ct);

    [HttpGet("conductores")]
    public async Task<IReadOnlyList<ConductorAsignadoReporte>> Conductores(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct) =>
        await servicio.ConductoresAsignadosAsync(desde, hasta, ct);

    [HttpGet("clientes")]
    public async Task<IReadOnlyList<ClienteOperacionReporte>> Clientes(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct) =>
        await servicio.OperacionPorClienteAsync(desde, hasta, ct);

    [HttpGet("indicadores")]
    public async Task<IndicadoresResponse> Indicadores(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct) =>
        await servicio.IndicadoresAsync(desde, hasta, ct);
}
