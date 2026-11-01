using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using Microsoft.Extensions.Configuration;

namespace FlowRide.Comunicacion.Infraestructura.Meta;

internal sealed class ClienteMeta(HttpClient http, IConfiguration configuracion) : IWhatsAppGateway
{
    private string Version => configuracion["WhatsApp:VersionApi"] ?? "v21.0";

    public async Task EnviarAsync(string telefono, string texto, CancellationToken ct = default)
    {
        var identificador = configuracion["WhatsApp:PhoneNumberId"] ?? throw new InvalidOperationException("Falta 'WhatsApp:PhoneNumberId'.");
        using var peticion = Peticion(HttpMethod.Post, $"{Version}/{identificador}/messages");
        peticion.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to = telefono.TrimStart('+'),
            type = "text",
            text = new { body = texto },
        });

        using var respuesta = await http.SendAsync(peticion, ct);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DescargarAsync(string mediaId, CancellationToken ct = default)
    {
        using var consulta = Peticion(HttpMethod.Get, $"{Version}/{mediaId}");
        using var datos = await http.SendAsync(consulta, ct);
        datos.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await datos.Content.ReadAsStringAsync(ct));
        var url = json.RootElement.GetProperty("url").GetString()!;

        using var descarga = Peticion(HttpMethod.Get, url);
        using var archivo = await http.SendAsync(descarga, ct);
        archivo.EnsureSuccessStatusCode();

        return await archivo.Content.ReadAsByteArrayAsync(ct);
    }

    private HttpRequestMessage Peticion(HttpMethod metodo, string ruta)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracion["WhatsApp:AccessToken"]);

        return peticion;
    }
}
