using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Tenancy;
using Microsoft.AspNetCore.Authentication;

namespace FlowRide.Inicializador.Seguridad;

internal static class RegistroSeguridad
{
    public static IServiceCollection AgregarSeguridad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddHttpContextAccessor();
        servicios.AddScoped<ContextoActor>();
        servicios.AddScoped<IContextoActor>(sp => sp.GetRequiredService<ContextoActor>());
        servicios.AddScoped<IContextoActorEstablecible>(sp => sp.GetRequiredService<ContextoActor>());
        servicios.AddScoped<IClaimsTransformation, TransformadorUsuario>();

        var tenant = configuracion["Tenant:Id"];
        servicios.AddSingleton<IContextoTenant>(
            new ContextoTenant(Guid.TryParse(tenant, out var id) ? id : throw new InvalidOperationException("Falta 'Tenant:Id' (GUID del operador de este despliegue).")));

        return servicios;
    }
}
