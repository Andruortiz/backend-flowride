using System.Text.Json;
using System.Text.RegularExpressions;
using FlowRide.Flota.Dominio;
using FlowRide.Mensajes;

namespace FlowRide.Pruebas.Arquitectura;

public partial class MensajesNoQuemados
{
    private static readonly string Fuentes = Path.Combine(RaizRepositorio(), "src");

    private static readonly HashSet<string> Claves = CargarClaves();

    private static string RaizRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "FlowRide.slnx")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    private static HashSet<string> CargarClaves()
    {
        var ruta = Path.Combine(Fuentes, "Compartido", "FlowRide.Mensajes", "Recursos", $"{CatalogoMensajes.IdiomaPorDefecto}.json");

        return [.. JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ruta))!.Keys];
    }

    private static IEnumerable<(string Archivo, string Texto)> Codigo() =>
        Directory.EnumerateFiles(Fuentes, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}Migraciones{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (Path.GetRelativePath(Fuentes, f), File.ReadAllText(f)));

    [GeneratedRegex(@"(?:(?<![A-Za-z])(?:Domain|Conflict|NotFound|Forbidden)Exception|MensajeClave)\(\s*""([^""]*)""|\.Texto\(\s*""([^""]*)""")]
    private static partial Regex ClavesUsadas();

    [GeneratedRegex(@"(?:(?<![A-Za-z])(?:Domain|Conflict|NotFound|Forbidden)Exception|MensajeClave)\(\s*\$")]
    private static partial Regex LiteralesInterpolados();

    [Fact]
    public void TodaClaveUsadaEnElCodigoExisteEnElCatalogo()
    {
        var faltantes = Codigo()
            .SelectMany(c => ClavesUsadas().Matches(c.Texto).Select(m => (c.Archivo, Clave: m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)))
            .Where(u => !Claves.Contains(u.Clave))
            .Select(u => $"{u.Archivo}: {u.Clave}")
            .ToList();

        Assert.True(faltantes.Count == 0, string.Join("\n", faltantes));
    }

    [Fact]
    public void LasExcepcionesDeNegocioNoLlevanTextoLiteral()
    {
        var literales = Codigo()
            .SelectMany(c => ClavesUsadas().Matches(c.Texto).Select(m => (c.Archivo, Clave: m.Groups[1].Value)).Where(u => u.Clave.Length > 0 && u.Clave.Contains(' ', StringComparison.Ordinal)))
            .Concat(Codigo().Where(c => LiteralesInterpolados().IsMatch(c.Texto)).Select(c => (c.Archivo, Clave: "texto interpolado")))
            .Select(u => $"{u.Archivo}: {u.Clave}")
            .ToList();

        Assert.True(literales.Count == 0, string.Join("\n", literales));
    }

    [Fact]
    public void ElDominioNoUsaElCatalogoDirectamente()
    {
        var usos = Codigo()
            .Where(c => c.Archivo.Contains($"{Path.DirectorySeparatorChar}Dominio{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && c.Texto.Contains("ICatalogoMensajes", StringComparison.Ordinal))
            .Select(c => c.Archivo)
            .ToList();

        Assert.True(usos.Count == 0, string.Join("\n", usos));
    }

    [Fact]
    public void CadaMotivoDeNoAsignableTieneMensaje()
    {
        var faltantes = Enum.GetNames<MotivoNoAsignable>().Where(n => !Claves.Contains($"motivo_no_asignable.{n}")).ToList();

        Assert.True(faltantes.Count == 0, string.Join(", ", faltantes));
    }

    [Fact]
    public void LasVariablesDeUnaExcepcionExistenEnLaPlantilla()
    {
        var catalogo = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(Fuentes, "Compartido", "FlowRide.Mensajes", "Recursos", $"{CatalogoMensajes.IdiomaPorDefecto}.json")))!;
        var errores = new List<string>();

        foreach (var (archivo, texto) in Codigo())
        {
            foreach (Match m in LlamadasConVariables().Matches(texto))
            {
                var clave = m.Groups["clave"].Value;
                var plantilla = catalogo.GetValueOrDefault(clave);

                if (plantilla is null)
                {
                    continue;
                }

                foreach (Match v in NombreVariable().Matches(m.Groups["variables"].Value))
                {
                    if (!plantilla.Contains("{" + v.Groups[1].Value + "}", StringComparison.Ordinal))
                    {
                        errores.Add($"{archivo}: '{clave}' no usa {{{v.Groups[1].Value}}}");
                    }
                }
            }
        }

        Assert.True(errores.Count == 0, string.Join("\n", errores));
    }

    [GeneratedRegex(@"(?<![A-Za-z])(?:Domain|Conflict|NotFound|Forbidden)Exception\(\s*""(?<clave>[^""]+)""(?<variables>(?:\s*,\s*\(""[A-Za-z]+"",[^;]*?\))*)\)")]
    private static partial Regex LlamadasConVariables();

    [GeneratedRegex(@"\(""([A-Za-z]+)"",")]
    private static partial Regex NombreVariable();
}
