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
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Plataforma.Infraestructura.Repositorios;

namespace FlowRide.Plataforma.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/parametros")]
public sealed class ParametrosController(IServicioParametros servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<ParametroResponse>> Listar(CancellationToken ct) => await servicio.ListarAsync(ct);

    [HttpPut("{clave}")]
    [Authorize(Politicas.Administracion)]
    public async Task<ParametroResponse> Actualizar(string clave, ActualizarParametroRequest request, CancellationToken ct) =>
        await servicio.ActualizarAsync(clave, request, ct);
}

[ApiController]
[Route("api/v1/operador")]
public sealed class OperadorController(IServicioOperador servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<OperadorResponse> Obtener(CancellationToken ct) => await servicio.ObtenerAsync(ct);

    [HttpPut]
    [Authorize(Politicas.Administracion)]
    public async Task<OperadorResponse> Actualizar(ActualizarOperadorRequest request, CancellationToken ct) =>
        await servicio.ActualizarAsync(request, ct);
}

[ApiController]
[Route("api/v1/auditoria")]
public sealed class AuditoriaController(IServicioAuditoria servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.Administracion)]
    public async Task<IReadOnlyList<AuditoriaResponse>> Consultar(
        string? accion, string? recurso, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ConsultarAsync(accion, recurso, desde, hasta, pagina, tamano, ct);
}
