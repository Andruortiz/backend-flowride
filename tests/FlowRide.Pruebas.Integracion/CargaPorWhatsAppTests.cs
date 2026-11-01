using System.Globalization;
using System.Net.Http.Json;
using System.Text;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class CargaPorWhatsAppTests(FabricaApi api)
{
    private const string Telefono = "573000000101";

    private static string Fecha(int dias) => DateTime.UtcNow.AddDays(dias).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<string> EnviarAsync(HttpClient cliente, string texto, string? csv = null)
    {
        var mensaje = new
        {
            externalMessageId = $"wamid.{Guid.NewGuid()}",
            telefono = Telefono,
            texto,
            documento = csv is null ? null : new { nombre = "itinerario.csv", contenido = Encoding.UTF8.GetBytes(csv) },
        };

        var respuesta = await FabricaApi.LeerAsync(await cliente.PostAsJsonAsync("/api/v1/webhooks/whatsapp", mensaje));

        return respuesta.GetProperty("texto").GetString()!;
    }

    [Fact]
    public async Task ElPasajeroCargaSuItinerarioPorWhatsAppConUnArchivoYLoConfirma()
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Webhook-Secret", FabricaApi.SecretoWebhook);

        var inicio = await EnviarAsync(cliente, "itinerario");

        if (inicio.Contains("domicilio", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("Guardé tu domicilio", await EnviarAsync(cliente, "Calle 1 # 2-3, Bogotá"));
        }

        var conErrores = await EnviarAsync(cliente, "archivo", "codigo_servicio,fecha,hora_salida,hora_llegada,destino\nBT200,no-es-fecha,04:30,05:30,Aeropuerto El Dorado");
        Assert.Contains("errores", conErrores);

        var resumen = await EnviarAsync(
            cliente,
            "archivo",
            $"codigo_servicio,fecha,hora_salida,hora_llegada,destino\nBT201,{Fecha(12)},04:30,05:30,Aeropuerto El Dorado\nBT202,{Fecha(13)},05:00,06:00,Aeropuerto El Dorado");
        Assert.Contains("Leí 2 viajes", resumen);

        var final = await EnviarAsync(cliente, "confirmar");
        Assert.Contains("cargué 2 viajes", final);

        var repetida = await EnviarAsync(cliente, "confirmar");
        Assert.DoesNotContain("cargué", repetida);
    }
}
