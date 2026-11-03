using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Mensajes;

public static class Registro
{
    public static IServiceCollection AgregarCatalogoMensajes(this IServiceCollection servicios) =>
        servicios.AddSingleton<ICatalogoMensajes>(new CatalogoMensajes());
}
