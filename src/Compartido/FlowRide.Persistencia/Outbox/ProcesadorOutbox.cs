using System.Text.Json;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Eventos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowRide.Persistencia.Outbox;

public sealed class OpcionesOutbox
{
    public int IntervaloSegundos { get; set; } = 2;

    public int MaximoIntentos { get; set; } = 8;
}

public sealed class DespachadorEventos
{
    private static readonly Dictionary<string, Type> Tipos = typeof(FlowRide.Contratos.UsuarioDesactivado).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && typeof(IEventoDominio).IsAssignableFrom(t))
        .ToDictionary(t => t.FullName!);

    public async Task DespacharAsync(IServiceProvider servicios, string tipo, string carga, CancellationToken ct)
    {
        if (!Tipos.TryGetValue(tipo, out var tipoEvento))
        {
            throw new InvalidOperationException($"El outbox contiene un evento desconocido: {tipo}.");
        }

        var evento = JsonSerializer.Deserialize(carga, tipoEvento, ColumnasJson.Opciones)!;
        var contrato = typeof(IManejadorEvento<>).MakeGenericType(tipoEvento);
        var metodo = contrato.GetMethod(nameof(IManejadorEvento<IEventoDominio>.ManejarAsync))!;

        foreach (var manejador in servicios.GetServices(contrato))
        {
            await (Task)metodo.Invoke(manejador, [evento, ct])!;
        }
    }
}

public sealed partial class ProcesadorOutbox(
    IServiceScopeFactory fabrica,
    DespachadorEventos despachador,
    Microsoft.Extensions.Options.IOptions<OpcionesOutbox> opciones,
    TimeProvider reloj,
    ILogger<ProcesadorOutbox> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcesarSiguienteAsync(stoppingToken))
                {
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                CicloFallido(logger, ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(opciones.Value.IntervaloSegundos), stoppingToken);
        }
    }

    public async Task<bool> ProcesarSiguienteAsync(CancellationToken ct)
    {
        await using var alcanceCola = fabrica.CreateAsyncScope();
        var db = alcanceCola.ServiceProvider.GetRequiredService<FlowRideDbContext>();
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var pendientes = await db.Outbox
            .FromSqlRaw(
                "SELECT * FROM ops.outbox WHERE procesado_en IS NULL AND intentos < {0} AND disponible_desde <= now() " +
                "ORDER BY ocurrio_en, id LIMIT 1 FOR UPDATE SKIP LOCKED",
                opciones.Value.MaximoIntentos)
            .ToListAsync(ct);

        if (pendientes.Count == 0)
        {
            return false;
        }

        var mensaje = pendientes[0];

        try
        {
            await using var alcanceManejadores = fabrica.CreateAsyncScope();
            await despachador.DespacharAsync(alcanceManejadores.ServiceProvider, mensaje.Tipo, mensaje.Carga, ct);

            mensaje.ProcesadoEn = reloj.GetUtcNow();
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            await RegistrarFalloAsync(mensaje.Id, ex, ct);
        }

        return true;
    }

    private async Task RegistrarFalloAsync(Guid id, Exception error, CancellationToken ct)
    {
        await using var alcance = fabrica.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<FlowRideDbContext>();
        var mensaje = await db.Outbox.FirstAsync(m => m.Id == id, ct);

        mensaje.Intentos++;
        mensaje.UltimoError = error.Message.Length > 1900 ? error.Message[..1900] : error.Message;
        mensaje.DisponibleDesde = reloj.GetUtcNow().AddSeconds(Math.Min(Math.Pow(2, mensaje.Intentos), 300));
        await db.SaveChangesAsync(ct);

        EventoFallido(logger, mensaje.Tipo, mensaje.Intentos, error);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "El ciclo del outbox falló")]
    private static partial void CicloFallido(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Evento {Tipo} falló (intento {Intentos}); se reintentará")]
    private static partial void EventoFallido(ILogger logger, string tipo, int intentos, Exception ex);
}
