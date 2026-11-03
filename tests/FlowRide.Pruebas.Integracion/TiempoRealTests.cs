using System.Net.Http.Json;
using System.Text.Json;
using FlowRide.Contratos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class TiempoRealTests(FabricaApi api)
{
    private async Task<string> TokenAsync(string codigoInterno)
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/dev/token", new { codigoInterno });
        respuesta.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;
    }

    private HubConnection Conectar(string? token) =>
        new HubConnectionBuilder()
            .WithUrl(
                new Uri(api.Server.BaseAddress, "/api/v1/tiempo-real"),
                o =>
                {
                    o.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                    o.Transports = HttpTransportType.LongPolling;
                    o.AccessTokenProvider = () => Task.FromResult(token);
                })
            .Build();

    [Fact]
    public async Task UnOperadorConectadoRecibeElTemaCuandoLlegaUnaUbicacion()
    {
        var recibido = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var conexion = Conectar(await TokenAsync("OPL-001"));
        conexion.On<string>("cambio", tema => recibido.TrySetResult(tema));
        await conexion.StartAsync();

        using var alcance = api.Services.CreateScope();
        var vehiculo = (await alcance.ServiceProvider.GetRequiredService<IConsultaFlota>().ListarVehiculosAsync())[2];
        await alcance.ServiceProvider.GetRequiredService<IServicioSeguimiento>().IngerirAsync(
            [new ReportarUbicacionRequest(vehiculo.Id, DateTimeOffset.UtcNow.AddSeconds(-3), 6.25, -75.57, "tiempo-real-prueba", false)]);

        var tema = await recibido.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("ubicaciones", tema);
    }

    [Fact]
    public void LosEventosDeNegocioTienenUnManejadorDeTiempoReal()
    {
        using var alcance = api.Services.CreateScope();
        var servicios = alcance.ServiceProvider;

        Assert.Contains(servicios.GetServices<IManejadorEvento<SolicitudRegistrada>>(), m => m.GetType().Name.StartsWith("ManejadorTiempoReal", StringComparison.Ordinal));
        Assert.Contains(servicios.GetServices<IManejadorEvento<ViajeIniciado>>(), m => m.GetType().Name.StartsWith("ManejadorTiempoReal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SinTokenNoSePuedeAbrirLaConexion()
    {
        await using var conexion = Conectar(null);

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => conexion.StartAsync());
    }

    [Fact]
    public async Task UnPasajeroNoPuedeEscucharLosCambiosDelOperador()
    {
        await using var conexion = Conectar(await TokenAsync("PAS-001"));

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => conexion.StartAsync());
    }
}
