using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using FlowRide.Viajes.Aplicacion;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Infraestructura.Repositorios;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Viajes.Infraestructura;

public static class Registro
{
    /// <summary>Registra los servicios, consultas, repositorios y manejadores de eventos del módulo.</summary>
    public static IServiceCollection AgregarModuloViajes(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<ICalificacionRepository, CalificacionRepository>();
        servicios.AddScoped<IConsultaViajes, ConsultaViajes>();
        servicios.AddScoped<IManejadorEvento<SolicitudCancelada>, AlCancelarSolicitud>();
        servicios.AddScoped<IProgramacionViajes, ProgramacionViajes>();
        servicios.AddScoped<IServicioSeguimiento, ServicioSeguimiento>();
        servicios.AddScoped<IServicioViajes, ServicioViajes>();
        servicios.AddScoped<IUbicacionRepository, UbicacionRepository>();
        servicios.AddScoped<IViajeRepository, ViajeRepository>();

        return servicios;
    }

    /// <summary>Solo para la fábrica de migraciones (dotnet ef): las tablas del módulo.</summary>
    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
