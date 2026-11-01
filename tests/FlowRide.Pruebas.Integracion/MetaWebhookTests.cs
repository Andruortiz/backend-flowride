using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class MetaWebhookTests(FabricaApi api)
{
    private const string Ruta = "/api/v1/webhooks/meta";

    private static string Firmar(string cuerpo) =>
        "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(FabricaApi.SecretoMeta), Encoding.UTF8.GetBytes(cuerpo)));

    private static string Payload(string id, string texto) =>
        $$$"""{"entry":[{"changes":[{"value":{"messages":[{"id":"{{{id}}}","from":"573000000102","type":"text","text":{"body":"{{{texto}}}"}}]}}]}]}""";

    private Task<HttpResponseMessage> EnviarAsync(string cuerpo, string? firma)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, Ruta) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };

        if (firma is not null)
        {
            peticion.Headers.Add("X-Hub-Signature-256", firma);
        }

        return api.CreateClient().SendAsync(peticion);
    }

    [Fact]
    public async Task LaVerificacionDevuelveElDesafioSoloConElTokenCorrecto()
    {
        var cliente = api.CreateClient();

        var correcta = await cliente.GetAsync($"{Ruta}?hub.mode=subscribe&hub.verify_token={FabricaApi.TokenVerificacion}&hub.challenge=12345");
        Assert.Equal("12345", await correcta.Content.ReadAsStringAsync());

        var incorrecta = await cliente.GetAsync($"{Ruta}?hub.mode=subscribe&hub.verify_token=otro&hub.challenge=12345");
        Assert.Equal(HttpStatusCode.Forbidden, incorrecta.StatusCode);
    }

    [Fact]
    public async Task UnMensajeSinFirmaValidaSeRechaza()
    {
        var cuerpo = Payload("wamid.meta-1", "estado");

        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(cuerpo, null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(cuerpo, "sha256=00")).StatusCode);
    }

    [Fact]
    public async Task UnMensajeFirmadoSeProcesaUnaSolaVez()
    {
        var cuerpo = Payload($"wamid.meta-{Guid.NewGuid()}", "estado");

        Assert.Equal(HttpStatusCode.OK, (await EnviarAsync(cuerpo, Firmar(cuerpo))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EnviarAsync(cuerpo, Firmar(cuerpo))).StatusCode);
    }
}
