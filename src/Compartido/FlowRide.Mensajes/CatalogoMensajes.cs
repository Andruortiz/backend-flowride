using System.Text.Json;

namespace FlowRide.Mensajes;

public sealed class CatalogoMensajes : ICatalogoMensajes
{
    public const string IdiomaPorDefecto = "es-CO";

    private readonly Dictionary<string, string> _textos;

    public CatalogoMensajes(string idioma = IdiomaPorDefecto)
    {
        var recurso = $"{typeof(CatalogoMensajes).Namespace}.Recursos.{idioma}.json";
        using var flujo = typeof(CatalogoMensajes).Assembly.GetManifestResourceStream(recurso)
            ?? throw new InvalidOperationException($"No existe el catálogo de mensajes para '{idioma}'.");

        _textos = JsonSerializer.Deserialize<Dictionary<string, string>>(flujo)!;
    }

    public bool Contiene(string clave) => _textos.ContainsKey(clave);

    public string Texto(string clave, IReadOnlyDictionary<string, string>? variables = null)
    {
        if (!_textos.TryGetValue(clave, out var texto))
        {
            throw new KeyNotFoundException($"No existe el mensaje '{clave}'.");
        }

        if (variables is null)
        {
            return texto;
        }

        foreach (var (nombre, valor) in variables)
        {
            texto = texto.Replace("{" + nombre + "}", valor, StringComparison.Ordinal);
        }

        return texto;
    }
}
