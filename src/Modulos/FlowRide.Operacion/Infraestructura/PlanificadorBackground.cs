using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using FlowRide.Operacion.Infraestructura.Repositorios;
using FlowRide.Operacion.Infraestructura.Controladores;
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

namespace FlowRide.Operacion.Infraestructura;

public sealed class OpcionesPlanificador
{
    public int IntervaloSegundos { get; set; } = 60;
}

/// <summary>Dispara el ciclo de tareas programadas (recordatorios, alertas de espera y de documentos).</summary>
public sealed partial class PlanificadorBackground(
    IServiceScopeFactory fabrica,
    IOptions<OpcionesPlanificador> opciones,
    ILogger<PlanificadorBackground> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var alcance = fabrica.CreateAsyncScope();
                var resultado = await alcance.ServiceProvider.GetRequiredService<IServicioPlanificacion>().EjecutarAsync(stoppingToken);
                CicloTerminado(logger, resultado.Recordatorios, resultado.AlertasSinConductor, resultado.AlertasDocumentos);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                CicloFallido(logger, ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(opciones.Value.IntervaloSegundos), stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Planificación: {Recordatorios} recordatorios, {SinConductor} esperas, {Documentos} documentos")]
    private static partial void CicloTerminado(ILogger logger, int recordatorios, int sinConductor, int documentos);

    [LoggerMessage(Level = LogLevel.Error, Message = "El ciclo de planificación falló")]
    private static partial void CicloFallido(ILogger logger, Exception ex);
}
