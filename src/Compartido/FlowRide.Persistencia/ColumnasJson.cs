using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowRide.Persistencia;

public static class ColumnasJson
{
    public static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serializar<T>(T valor) => JsonSerializer.Serialize(valor, Opciones);

    public static T Deserializar<T>(string json) => JsonSerializer.Deserialize<T>(json, Opciones)!;

    public static PropertyBuilder<T> ComoJson<T>(this PropertyBuilder<T> propiedad) =>
        propiedad
            .HasColumnType("jsonb")
            .HasConversion(
                v => Serializar(v),
                s => Deserializar<T>(s),
                new ValueComparer<T>(
                    (a, b) => Serializar(a) == Serializar(b),
                    v => Serializar(v).GetHashCode(StringComparison.Ordinal),
                    v => Deserializar<T>(Serializar(v))));
}
