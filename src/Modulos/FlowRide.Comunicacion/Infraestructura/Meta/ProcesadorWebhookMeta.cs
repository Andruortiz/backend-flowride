using System.Text.Json;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Viajes.Aplicacion.Puertos;
using Microsoft.Extensions.Logging;

namespace FlowRide.Comunicacion.Infraestructura.Meta;

internal sealed partial class ProcesadorWebhookMeta(
    IServicioConversaciones conversaciones,
    IServicioSeguimiento seguimiento,
    AdaptadorUbicacionWhatsApp ubicaciones,
    ClienteMeta meta,
    ILogger<ProcesadorWebhookMeta> logger) : IProcesadorWebhookMeta
{
    public async Task ProcesarAsync(byte[] cuerpo, CancellationToken ct = default)
    {
        using var json = JsonDocument.Parse(cuerpo);

        foreach (var mensaje in Mensajes(json.RootElement))
        {
            var id = mensaje.GetProperty("id").GetString()!;

            try
            {
                var tipo = mensaje.TryGetProperty("type", out var t) ? t.GetString() : null;

                if (tipo == "location")
                {
                    await seguimiento.IngerirAsync(await ubicaciones.NormalizarAsync(mensaje, ct), ct);
                    continue;
                }

                var telefono = "+" + mensaje.GetProperty("from").GetString();
                var texto = tipo == "text" ? mensaje.GetProperty("text").GetProperty("body").GetString() ?? string.Empty : string.Empty;
                DocumentoEntrante? documento = null;

                if (tipo == "document")
                {
                    var adjunto = mensaje.GetProperty("document");
                    var nombre = adjunto.TryGetProperty("filename", out var f) ? f.GetString() ?? "archivo" : "archivo";
                    documento = new DocumentoEntrante(nombre, await meta.DescargarAsync(adjunto.GetProperty("id").GetString()!, ct));
                }

                await conversaciones.ProcesarAsync(new MensajeEntranteRequest(id, telefono, texto, documento), ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                Fallo(logger, id, ex);
            }
        }
    }

    private static IEnumerable<JsonElement> Mensajes(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("entry", out var entradas))
        {
            yield break;
        }

        foreach (var entrada in entradas.EnumerateArray())
        {
            foreach (var cambio in entrada.GetProperty("changes").EnumerateArray())
            {
                if (cambio.GetProperty("value").TryGetProperty("messages", out var mensajes))
                {
                    foreach (var mensaje in mensajes.EnumerateArray())
                    {
                        yield return mensaje;
                    }
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo procesar el mensaje de Meta {MensajeId}")]
    private static partial void Fallo(ILogger logger, string mensajeId, Exception ex);
}
