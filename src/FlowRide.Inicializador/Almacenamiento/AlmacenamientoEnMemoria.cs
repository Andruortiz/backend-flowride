using System.Collections.Concurrent;
using FlowRide.Nucleo.Almacenamiento;

namespace FlowRide.Inicializador.Almacenamiento;

internal sealed class AlmacenamientoEnMemoria : IObjectStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objetos = new();

    public async Task GuardarAsync(string ruta, Stream contenido, string tipoContenido, CancellationToken ct = default)
    {
        using var copia = new MemoryStream();
        await contenido.CopyToAsync(copia, ct);
        _objetos[ruta] = copia.ToArray();
    }

    public Task<Stream?> LeerAsync(string ruta, CancellationToken ct = default) =>
        Task.FromResult<Stream?>(_objetos.TryGetValue(ruta, out var bytes) ? new MemoryStream(bytes, writable: false) : null);
}
