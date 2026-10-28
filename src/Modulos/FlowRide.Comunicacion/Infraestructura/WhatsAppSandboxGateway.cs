using Microsoft.Extensions.Logging;
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
using FlowRide.Comunicacion.Infraestructura.Repositorios;
using FlowRide.Comunicacion.Infraestructura.Controladores;
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

namespace FlowRide.Comunicacion.Infraestructura;

/// <summary>
/// Reemplazo del proveedor real de WhatsApp mientras no esté contratado (ADR-0008): registra el envío sin salir de la máquina.
/// Se cambia por el adaptador del proveedor en el registro de servicios, sin tocar los casos de uso.
/// </summary>
internal sealed partial class WhatsAppSandboxGateway(ILogger<WhatsAppSandboxGateway> logger) : IWhatsAppGateway
{
    public Task EnviarAsync(string telefono, string texto, CancellationToken ct = default)
    {
        var enmascarado = Enmascarar(telefono);
        Enviado(logger, enmascarado, texto.Length);

        return Task.CompletedTask;
    }

    // El número es un dato personal: solo los últimos cuatro dígitos llegan a los logs.
    private static string Enmascarar(string telefono) =>
        telefono.Length <= 4 ? "****" : "****" + telefono[^4..];

    [LoggerMessage(Level = LogLevel.Information, Message = "WhatsApp (sandbox) a {Telefono}: mensaje de {Longitud} caracteres")]
    private static partial void Enviado(ILogger logger, string telefono, int longitud);
}
