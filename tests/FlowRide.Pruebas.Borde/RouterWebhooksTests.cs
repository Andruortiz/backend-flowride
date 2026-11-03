using System.Net;
using System.Security.Cryptography;
using System.Text;
using FlowRide.Borde;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Borde;

public sealed class RouterWebhooksTests : IDisposable
{
    private const string Secreto = "secreto-meta-pruebas";
    private const string Token = "token-verificacion-pruebas";
    private const string Numero = "111222333";

    private readonly StubOperador _operador = new();
    private readonly WebApplicationFactory<Program> _fabrica;

    public RouterWebhooksTests()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("Borde:AppSecret", Secreto);
            b.UseSetting("Borde:VerifyToken", Token);
            b.UseSetting("Borde:Operadores:0:PhoneNumberId", Numero);
            b.UseSetting("Borde:Operadores:0:Destino", "http://operador.test");
            b.ConfigureServices(s => s.AddHttpClient(RouterWebhooks.Cliente).ConfigurePrimaryHttpMessageHandler(() => _operador));
        });
    }

    public void Dispose()
    {
        _fabrica.Dispose();
        _operador.Dispose();
    }

    private static string Cuerpo(string numero) =>
        $$$"""{"entry":[{"changes":[{"value":{"metadata":{"phone_number_id":"{{{numero}}}"},"messages":[]}}]}]}""";

    private static string Firma(string cuerpo) =>
        "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secreto), Encoding.UTF8.GetBytes(cuerpo)));

    private Task<HttpResponseMessage> EnviarAsync(string cuerpo, string? firma)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, RouterWebhooks.Ruta)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
        };

        if (firma is not null)
        {
            peticion.Headers.TryAddWithoutValidation(RouterWebhooks.CabeceraFirma, firma);
        }

        return _fabrica.CreateClient().SendAsync(peticion);
    }

    [Fact]
    public async Task ElHandshakeDevuelveElChallengeConElTokenCorrecto()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync($"{RouterWebhooks.Ruta}?hub.mode=subscribe&hub.verify_token={Token}&hub.challenge=12345");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("12345", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ElHandshakeConTokenIncorrectoSeRechaza()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync($"{RouterWebhooks.Ruta}?hub.mode=subscribe&hub.verify_token=otro&hub.challenge=12345");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sha256=00")]
    public async Task UnaFirmaAusenteOInvalidaSeRechazaYNoSeReenvia(string? firma)
    {
        var respuesta = await EnviarAsync(Cuerpo(Numero), firma);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Empty(_operador.Recibidos);
    }

    [Fact]
    public async Task UnaFirmaValidaSeReenviaAlOperadorConCuerpoYFirmaIntactos()
    {
        var cuerpo = Cuerpo(Numero);

        var respuesta = await EnviarAsync(cuerpo, Firma(cuerpo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var recibido = Assert.Single(_operador.Recibidos);
        Assert.Equal("http://operador.test/api/v1/webhooks/meta", recibido.Url);
        Assert.Equal(cuerpo, recibido.Cuerpo);
        Assert.Equal(Firma(cuerpo), recibido.Firma);
    }

    [Fact]
    public async Task UnNumeroSinOperadorSeIgnoraSinReenviar()
    {
        var cuerpo = Cuerpo("999");

        var respuesta = await EnviarAsync(cuerpo, Firma(cuerpo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(_operador.Recibidos);
    }

    [Fact]
    public async Task SiElOperadorFallaSeDevuelveErrorParaQueMetaReintente()
    {
        _operador.Estado = HttpStatusCode.InternalServerError;
        var cuerpo = Cuerpo(Numero);

        var respuesta = await EnviarAsync(cuerpo, Firma(cuerpo));

        Assert.Equal(HttpStatusCode.BadGateway, respuesta.StatusCode);
    }

    [Fact]
    public async Task ElBordeAgregaCabecerasDeSeguridad()
    {
        var respuesta = await _fabrica.CreateClient().GetAsync("/health");

        Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", respuesta.Headers.GetValues("X-Frame-Options").Single());
    }

    private sealed class StubOperador : HttpMessageHandler
    {
        public List<(string Url, string Cuerpo, string Firma)> Recibidos { get; } = [];

        public HttpStatusCode Estado { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cuerpo = await request.Content!.ReadAsStringAsync(cancellationToken);
            Recibidos.Add((request.RequestUri!.ToString(), cuerpo, request.Headers.GetValues(RouterWebhooks.CabeceraFirma).Single()));

            return new HttpResponseMessage(Estado);
        }
    }
}
