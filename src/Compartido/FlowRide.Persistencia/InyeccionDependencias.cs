using FlowRide.Nucleo.Persistencia;
using FlowRide.Persistencia.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Persistencia;

public static class InyeccionDependencias
{
    public static IServiceCollection AgregarPersistencia(this IServiceCollection servicios, IConfiguration configuracion, string ensambladoMigraciones)
    {
        var conexion = configuracion.GetConnectionString("FlowRide")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:FlowRide' (variable de entorno o user-secrets).");

        servicios.AddSingleton(TimeProvider.System);
        servicios.AddDbContext<FlowRideDbContext>(o => o.UseNpgsql(conexion, n => n.MigrationsAssembly(ensambladoMigraciones)).UseSnakeCaseNamingConvention());
        servicios.AddScoped<IUnitOfWork, UnitOfWork>();

        servicios.Configure<OpcionesOutbox>(configuracion.GetSection("Outbox"));
        servicios.AddSingleton<DespachadorEventos>();

        if (configuracion.GetValue("Outbox:Habilitado", true))
        {
            servicios.AddHostedService<ProcesadorOutbox>();
        }

        return servicios;
    }
}
