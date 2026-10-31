using FlowRide.Contratos;
using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Flota.Infraestructura.Repositorios;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Flota.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloFlota(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IConductorRepository, ConductorRepository>();
        servicios.AddScoped<IConsultaFlota, ConsultaFlota>();
        servicios.AddScoped<IManejadorEvento<UsuarioDesactivado>, AlDesactivarUsuario>();
        servicios.AddScoped<IServicioFlota, ServicioFlota>();
        servicios.AddScoped<IVehiculoRepository, VehiculoRepository>();

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
