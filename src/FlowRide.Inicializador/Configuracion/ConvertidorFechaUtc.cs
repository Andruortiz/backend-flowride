using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowRide.Inicializador.Configuracion;

/// <summary>
/// Todo instante entra y sale en UTC. PostgreSQL (timestamptz) rechaza offsets distintos de cero, y el sistema
/// opera con instantes: la zona del operador solo se usa para fechas de negocio.
/// </summary>
internal sealed class ConvertidorFechaUtc : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime());
}
