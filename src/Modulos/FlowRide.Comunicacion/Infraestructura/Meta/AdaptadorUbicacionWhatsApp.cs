using System.Text.Json;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Infraestructura.Meta;

internal sealed class AdaptadorUbicacionWhatsApp(IConsultaIdentidad identidad, IConsultaFlota flota) : ITelemetryProviderAdapter
{
    public const string FuenteWhatsApp = "whatsapp";

    public string Fuente => FuenteWhatsApp;

    public async Task<IReadOnlyList<ReportarUbicacionRequest>> NormalizarAsync(JsonElement carga, CancellationToken ct = default)
    {
        var telefono = "+" + carga.GetProperty("from").GetString();
        var ubicacion = carga.GetProperty("location");
        var segundos = long.Parse(carga.GetProperty("timestamp").GetString()!, System.Globalization.CultureInfo.InvariantCulture);

        var usuario = await identidad.ObtenerUsuarioPorTelefonoAsync(telefono, ct);
        var conductor = usuario is null ? null : await flota.ObtenerConductorPorUsuarioAsync(usuario.Id, ct);

        if (conductor?.VehiculoId is not { } vehiculoId)
        {
            return [];
        }

        return
        [
            new ReportarUbicacionRequest(
                vehiculoId,
                DateTimeOffset.FromUnixTimeSeconds(segundos),
                ubicacion.GetProperty("latitude").GetDouble(),
                ubicacion.GetProperty("longitude").GetDouble(),
                Fuente,
                Continua: false),
        ];
    }
}
