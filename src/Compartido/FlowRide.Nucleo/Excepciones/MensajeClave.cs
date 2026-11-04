using System.Globalization;

namespace FlowRide.Nucleo.Excepciones;

public sealed record MensajeClave(string Clave, IReadOnlyDictionary<string, string> Variables)
{
    public MensajeClave(string clave, params (string Nombre, object? Valor)[] variables)
        : this(clave, variables.ToDictionary(v => v.Nombre, v => Convert.ToString(v.Valor, CultureInfo.InvariantCulture) ?? string.Empty))
    {
    }
}
