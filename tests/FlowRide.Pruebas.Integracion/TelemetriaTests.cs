using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Persistencia;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class TelemetriaTests(FabricaApi api)
{
    private const string TelefonoConductor = "573000000201";

    private static string Firmar(string cuerpo) =>
        "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(FabricaApi.SecretoMeta), Encoding.UTF8.GetBytes(cuerpo)));

    private static string Ubicacion(string telefono, long segundos, double latitud, double longitud) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $$$"""{"entry":[{"changes":[{"value":{"messages":[{"id":"wamid.{{{Guid.NewGuid()}}}","from":"{{{telefono}}}","timestamp":"{{{segundos}}}","type":"location","location":{"latitude":{{{latitud}}},"longitude":{{{longitud}}}}}]}}]}]}""");

    private async Task<HttpStatusCode> EnviarAsync(string cuerpo)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/meta") { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
        peticion.Headers.Add("X-Hub-Signature-256", Firmar(cuerpo));

        return (await api.CreateClient().SendAsync(peticion)).StatusCode;
    }

    private async Task<long> ContarAsync()
    {
        using var alcance = api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<FlowRideDbContext>();

        return await db.Database.SqlQuery<long>($"SELECT count(*) AS \"Value\" FROM ops.ubicaciones WHERE fuente = 'whatsapp'").SingleAsync();
    }

    [Fact]
    public async Task UnaUbicacionDeWhatsAppDeUnConductorSeGuardaUnaSolaVez()
    {
        var antes = await ContarAsync();
        var segundos = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cuerpo = Ubicacion(TelefonoConductor, segundos, 6.2442, -75.5812);

        Assert.Equal(HttpStatusCode.OK, await EnviarAsync(cuerpo));
        Assert.Equal(HttpStatusCode.OK, await EnviarAsync(Ubicacion(TelefonoConductor, segundos, 6.2442, -75.5812)));

        Assert.Equal(antes + 1, await ContarAsync());
    }

    [Fact]
    public async Task UnaUbicacionDeQuienNoEsConductorSeIgnora()
    {
        var antes = await ContarAsync();

        var estado = await EnviarAsync(Ubicacion("573000000101", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 6.2, -75.5));

        Assert.Equal(HttpStatusCode.OK, estado);
        Assert.Equal(antes, await ContarAsync());
    }

    [Fact]
    public async Task UnEventoInvalidoSeAislaYLosDemasSeGuardan()
    {
        using var alcance = api.Services.CreateScope();
        var vehiculo = (await alcance.ServiceProvider.GetRequiredService<IConsultaFlota>().ListarVehiculosAsync())[0];
        var seguimiento = alcance.ServiceProvider.GetRequiredService<IServicioSeguimiento>();
        var ahora = DateTimeOffset.UtcNow;

        var resultado = await seguimiento.IngerirAsync(
        [
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-30), 6.20, -75.50, "prueba-isolada", false),
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-20), 99, -75.50, "prueba-isolada", false),
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-10), 6.21, -75.51, "prueba-isolada", false),
            new ReportarUbicacionRequest(vehiculo.Id, ahora.AddSeconds(-10), 6.21, -75.51, "prueba-isolada", false),
        ]);

        Assert.Equal(new ResultadoIngesta(2, 1, 1), resultado);
    }

    [Fact]
    public async Task LaTablaEstaParticionadaYElMantenimientoEliminaSoloLasParticionesVencidas()
    {
        using var alcance = api.Services.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<FlowRideDbContext>();
        var actual = $"ops.ubicaciones_{DateTime.UtcNow:yyyy_MM}";

        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS ops.ubicaciones_2020_01 PARTITION OF ops.ubicaciones FOR VALUES FROM ('2020-01-01 00:00:00+00') TO ('2020-02-01 00:00:00+00')");

        var eliminadas = await db.Database.SqlQuery<int>($"SELECT ops.mantener_particiones_ubicaciones(6) AS \"Value\"").SingleAsync();

        Assert.True(eliminadas >= 1);
        Assert.Null(await db.Database.SqlQuery<string?>($"SELECT to_regclass('ops.ubicaciones_2020_01')::text AS \"Value\"").SingleAsync());
        Assert.NotNull(await db.Database.SqlQuery<string?>($"SELECT to_regclass({actual})::text AS \"Value\"").SingleAsync());
        Assert.Equal("p", await db.Database.SqlQuery<string>($"SELECT relkind::text AS \"Value\" FROM pg_class WHERE oid = 'ops.ubicaciones'::regclass").SingleAsync());
    }
}
