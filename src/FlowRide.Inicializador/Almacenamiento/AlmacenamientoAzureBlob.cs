using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FlowRide.Nucleo.Almacenamiento;

namespace FlowRide.Inicializador.Almacenamiento;

internal sealed class AlmacenamientoAzureBlob(BlobContainerClient contenedor) : IObjectStorage
{
    private volatile bool _creado;

    public async Task GuardarAsync(string ruta, Stream contenido, string tipoContenido, CancellationToken ct = default)
    {
        await AsegurarContenedorAsync(ct);
        await contenedor.GetBlobClient(ruta).UploadAsync(contenido, new BlobHttpHeaders { ContentType = tipoContenido }, cancellationToken: ct);
    }

    public async Task<Stream?> LeerAsync(string ruta, CancellationToken ct = default)
    {
        await AsegurarContenedorAsync(ct);

        try
        {
            return await contenedor.GetBlobClient(ruta).OpenReadAsync(cancellationToken: ct);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    private async Task AsegurarContenedorAsync(CancellationToken ct)
    {
        if (_creado)
        {
            return;
        }

        await contenedor.CreateIfNotExistsAsync(cancellationToken: ct);
        _creado = true;
    }
}
