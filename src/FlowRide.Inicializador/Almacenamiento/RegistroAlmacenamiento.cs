using Azure.Storage.Blobs;
using FlowRide.Nucleo.Almacenamiento;

namespace FlowRide.Inicializador.Almacenamiento;

internal static class RegistroAlmacenamiento
{
    public static IServiceCollection AgregarAlmacenamiento(this IServiceCollection servicios, IConfiguration configuracion, bool esDesarrollo)
    {
        var conexion = configuracion["Almacenamiento:Conexion"];

        if (string.IsNullOrWhiteSpace(conexion))
        {
            if (!esDesarrollo)
            {
                throw new InvalidOperationException("Falta 'Almacenamiento:Conexion'. Sin almacenamiento de objetos el servicio no arranca.");
            }

            return servicios.AddSingleton<IObjectStorage, AlmacenamientoEnMemoria>();
        }

        var nombre = configuracion["Almacenamiento:Contenedor"] ?? "flowride";

        return servicios.AddSingleton<IObjectStorage>(_ => new AlmacenamientoAzureBlob(new BlobContainerClient(conexion, nombre)));
    }
}
