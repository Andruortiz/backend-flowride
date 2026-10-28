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
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Comunicacion.Infraestructura;
using FlowRide.Comunicacion.Infraestructura.Repositorios;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/alertas")]
public sealed class AlertasController(IServicioAlertas servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<AlertaResponse>> Listar(bool soloNoLeidas = true, int pagina = 1, int tamano = 50, CancellationToken ct = default) =>
        await servicio.ListarAsync(soloNoLeidas, pagina, tamano, ct);

    [HttpGet("no-leidas")]
    [Authorize(Politicas.Gestion)]
    public async Task<int> ContarNoLeidas(CancellationToken ct) => await servicio.ContarNoLeidasAsync(ct);

    [HttpPost("{id:guid}/leida")]
    [Authorize(Politicas.Gestion)]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        await servicio.MarcarLeidaAsync(id, ct);

        return NoContent();
    }
}

/// <summary>
/// Entrada de los mensajes de WhatsApp. No usa JWT: el proveedor se autentica con un secreto compartido.
/// Sin secreto configurado el endpoint no existe (falla cerrado). La verificación de firma del proveedor real queda con su integración.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/whatsapp")]
[AllowAnonymous]
public sealed class WhatsAppWebhookController(IServicioConversaciones servicio, IConfiguration configuracion) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir(MensajeEntranteRequest request, CancellationToken ct)
    {
        var secreto = configuracion["WhatsApp:SecretoWebhook"];

        if (string.IsNullOrEmpty(secreto))
        {
            return NotFound();
        }

        var recibido = Request.Headers["X-Webhook-Secret"].ToString();

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recibido), Encoding.UTF8.GetBytes(secreto)))
        {
            return Unauthorized();
        }

        return Ok(await servicio.ProcesarAsync(request, ct));
    }
}
