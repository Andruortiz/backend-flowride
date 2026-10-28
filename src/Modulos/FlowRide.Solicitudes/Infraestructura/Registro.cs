using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using FlowRide.Solicitudes.Aplicacion;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Infraestructura.Repositorios;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Solicitudes.Infraestructura;

public static class Registro
{
    /// <summary>Registra los servicios, consultas, repositorios y manejadores de eventos del módulo.</summary>
    public static IServiceCollection AgregarModuloSolicitudes(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IConsultaSolicitudes, ConsultaSolicitudes>();
        servicios.AddScoped<IManejadorEvento<ViajeFinalizado>, AlFinalizarViaje>();
        servicios.AddScoped<IManejadorEvento<ViajeIniciado>, AlIniciarViaje>();
        servicios.AddScoped<IServicioSolicitudes, ServicioSolicitudes>();
        servicios.AddScoped<ISolicitudRepository, SolicitudRepository>();

        return servicios;
    }

    /// <summary>Solo para la fábrica de migraciones (dotnet ef): las tablas del módulo.</summary>
    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
