using FlowRide.Persistencia;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowRide.Viajes.Infraestructura;

internal sealed class MantenimientoTelemetria(FlowRideDbContext db, IServicioParametros parametros)
{
    public async Task<int> EjecutarAsync(CancellationToken ct = default)
    {
        var retencion = await parametros.ValorAsync(CatalogoParametros.RetencionTelemetriaMeses, ct);

        return await db.Database
            .SqlQuery<int>($"SELECT ops.mantener_particiones_ubicaciones({retencion}) AS \"Value\"")
            .SingleAsync(ct);
    }
}

internal sealed partial class MantenimientoTelemetriaBackground(
    IServiceScopeFactory fabrica,
    IConfiguration configuracion,
    ILogger<MantenimientoTelemetriaBackground> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuracion.GetValue("Telemetria:MantenimientoHabilitado", true))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var alcance = fabrica.CreateAsyncScope();
                var eliminadas = await alcance.ServiceProvider.GetRequiredService<MantenimientoTelemetria>().EjecutarAsync(stoppingToken);
                Terminado(logger, eliminadas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Fallido(logger, ex);
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Mantenimiento de telemetría: {Eliminadas} particiones vencidas eliminadas")]
    private static partial void Terminado(ILogger logger, int eliminadas);

    [LoggerMessage(Level = LogLevel.Error, Message = "El mantenimiento de particiones de telemetría falló")]
    private static partial void Fallido(ILogger logger, Exception ex);
}
