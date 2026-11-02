using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace FlowRide.Borde;

public static partial class RouterWebhooks
{
    public const string Ruta = "/api/v1/webhooks/meta";
    public const string Cliente = "operador";
    public const string CabeceraFirma = "X-Hub-Signature-256";

    private static readonly Meter Medidor = new("FlowRide.Borde");
    private static readonly Counter<long> Rechazados = Medidor.CreateCounter<long>("flowride.borde.webhooks.rechazados");
    private static readonly Counter<long> Reenviados = Medidor.CreateCounter<long>("flowride.borde.webhooks.reenviados");

    public static IEndpointRouteBuilder MapWebhooksMeta(this IEndpointRouteBuilder rutas)
    {
        rutas.MapGet(Ruta, Verificar);
        rutas.MapPost(Ruta, ReenviarAsync);

        return rutas;
    }

    private static IResult Verificar(HttpRequest request, IOptionsMonitor<OpcionesBorde> opciones)
    {
        var esperado = opciones.CurrentValue.VerifyToken;
        var recibido = request.Query["hub.verify_token"].ToString();

        if (request.Query["hub.mode"] != "subscribe" || esperado.Length == 0 || !Iguales(recibido, esperado))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return Results.Text(request.Query["hub.challenge"].ToString());
    }

    private static async Task<IResult> ReenviarAsync(
        HttpRequest request,
        IOptionsMonitor<OpcionesBorde> opciones,
        IHttpClientFactory fabrica,
        ILoggerFactory registros,
        CancellationToken ct)
    {
        var logger = registros.CreateLogger("FlowRide.Borde.Webhooks");
        var configuracion = opciones.CurrentValue;

        using var memoria = new MemoryStream();
        await request.Body.CopyToAsync(memoria, ct);
        var cuerpo = memoria.ToArray();
        var firma = request.Headers[CabeceraFirma].ToString();

        if (configuracion.AppSecret.Length == 0 || !Iguales(firma, Firmar(configuracion.AppSecret, cuerpo)))
        {
            Rechazados.Add(1);
            FirmaInvalida(logger, firma.Length > 0);

            return Results.Unauthorized();
        }

        if (!TryNumeros(cuerpo, out var numeros))
        {
            return Results.BadRequest();
        }

        var destinos = configuracion.Operadores
            .Where(o => numeros.Contains(o.PhoneNumberId))
            .Select(o => o.Destino)
            .Distinct()
            .ToList();

        if (destinos.Count == 0)
        {
            SinOperador(logger);

            return Results.Ok();
        }

        var http = fabrica.CreateClient(Cliente);

        foreach (var destino in destinos)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(destino), Ruta))
            {
                Content = new ByteArrayContent(cuerpo),
            };
            peticion.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            peticion.Headers.TryAddWithoutValidation(CabeceraFirma, firma);

            try
            {
                using var respuesta = await http.SendAsync(peticion, ct);

                if (!respuesta.IsSuccessStatusCode)
                {
                    FalloDestino(logger, destino, (int)respuesta.StatusCode);

                    return Results.StatusCode(StatusCodes.Status502BadGateway);
                }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                FalloDestino(logger, destino, 0);

                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            Reenviados.Add(1);
        }

        return Results.Ok();
    }

    private static string Firmar(string secreto, byte[] cuerpo) =>
        "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), cuerpo));

    private static bool Iguales(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static bool TryNumeros(byte[] cuerpo, out HashSet<string> numeros)
    {
        numeros = [];

        try
        {
            var carga = JsonSerializer.Deserialize<Carga>(cuerpo, JsonSerializerOptions.Web);
            var ids = carga?.Entry?.SelectMany(e => e.Changes ?? []).Select(c => c.Value?.Metadata?.PhoneNumberId);
            numeros = [.. ids?.Where(id => !string.IsNullOrEmpty(id)).Select(id => id!) ?? []];

            return carga is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook de Meta rechazado por firma inválida (cabecera presente: {Presente}).")]
    private static partial void FirmaInvalida(ILogger logger, bool presente);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook de Meta sin operador configurado para su phone_number_id; se ignora.")]
    private static partial void SinOperador(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "El operador {Destino} no aceptó el webhook (estado {Estado}).")]
    private static partial void FalloDestino(ILogger logger, string destino, int estado);

    private sealed record Carga(List<Entrada>? Entry);

    private sealed record Entrada(List<Cambio>? Changes);

    private sealed record Cambio(Valor? Value);

    private sealed record Valor(Metadatos? Metadata);

    private sealed record Metadatos([property: JsonPropertyName("phone_number_id")] string? PhoneNumberId);
}
