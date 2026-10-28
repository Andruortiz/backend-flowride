using FlowRide.Contratos;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Infraestructura.Repositorios;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Identidad.Infraestructura;

public static class Registro
{
    /// <summary>Registra los servicios, consultas, repositorios y manejadores de eventos del módulo.</summary>
    public static IServiceCollection AgregarModuloIdentidad(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IConsultaIdentidad, ConsultaIdentidad>();
        servicios.AddScoped<IPasajeroRepository, PasajeroRepository>();
        servicios.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        servicios.AddScoped<IUsuarioRepository, UsuarioRepository>();

        return servicios;
    }

    /// <summary>Solo para la fábrica de migraciones (dotnet ef): las tablas del módulo.</summary>
    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
