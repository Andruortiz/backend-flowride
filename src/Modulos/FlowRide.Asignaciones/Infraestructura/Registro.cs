using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Asignaciones.Infraestructura.Repositorios;
using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Asignaciones.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloAsignaciones(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IAsignacionRepository, AsignacionRepository>();
        servicios.AddScoped<IConfiguracionAsignacionRepository, ConfiguracionAsignacionRepository>();
        servicios.AddScoped<IConsultaAsignaciones, ConsultaAsignaciones>();
        servicios.AddScoped<IManejadorEvento<AsignacionRechazada>, AlRechazarAsignacion>();
        servicios.AddScoped<IManejadorEvento<SolicitudCancelada>, AlCancelarSolicitud>();
        servicios.AddScoped<IManejadorEvento<SolicitudRegistrada>, AlRegistrarSolicitud>();
        servicios.AddScoped<IManejadorEvento<ViajeFinalizado>, AlFinalizarViaje>();
        servicios.AddScoped<IManejadorEvento<ViajeIniciado>, AlIniciarViaje>();
        servicios.AddScoped<IServicioAsignaciones, ServicioAsignaciones>();

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
