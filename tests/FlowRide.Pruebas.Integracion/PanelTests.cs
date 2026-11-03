using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class PanelTests(FabricaApi api)
{
    [Fact]
    public async Task LasUltimasUbicacionesDevuelvenSoloLaMasRecienteDeCadaVehiculo()
    {
        using var alcance = api.Services.CreateScope();
        var vehiculo = (await alcance.ServiceProvider.GetRequiredService<IConsultaFlota>().ListarVehiculosAsync())[1];
        var ahora = DateTimeOffset.UtcNow;

        await alcance.ServiceProvider.GetRequiredService<IServicioSeguimiento>().IngerirAsync(
        [
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-40), 6.10, -75.10, "panel-prueba", false),
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-5), 6.30, -75.30, "panel-prueba", false),
        ]);

        var respuesta = await FabricaApi.LeerAsync(await (await api.ClienteComoAsync("OPL-001")).GetAsync("/api/v1/ubicaciones/ultimas"));
        var propias = respuesta.EnumerateArray().Where(u => u.GetProperty("vehiculoId").GetGuid() == vehiculo.Id).ToList();

        var ultima = Assert.Single(propias);
        Assert.Equal(6.30, ultima.GetProperty("latitud").GetDouble());
    }

    [Fact]
    public async Task ElListadoDePasajerosIncluyeElNombre()
    {
        var respuesta = await FabricaApi.LeerAsync(await (await api.ClienteComoAsync("OPL-001")).GetAsync("/api/v1/pasajeros"));

        Assert.Contains(respuesta.EnumerateArray(), p => p.GetProperty("nombre").GetString() == "Pasajero 1");
    }
}
