namespace FlowRide.Nucleo.Almacenamiento;

public interface IObjectStorage
{
    Task GuardarAsync(string ruta, Stream contenido, string tipoContenido, CancellationToken ct = default);

    Task<Stream?> LeerAsync(string ruta, CancellationToken ct = default);
}
