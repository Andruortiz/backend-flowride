using FlowRide.Comunicacion.Aplicacion;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Comunicacion.Infraestructura.Repositorios;
using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Comunicacion.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloComunicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<ComposicionMensajes>();
        servicios.AddScoped<IAlertaRepository, AlertaRepository>();
        servicios.AddScoped<IConversacionRepository, ConversacionRepository>();
        servicios.AddScoped<IManejadorEvento<AsignacionAceptada>, AlAceptarAsignacion>();
        servicios.AddScoped<IManejadorEvento<AsignacionCreada>, AlCrearAsignacion>();
        servicios.AddScoped<IManejadorEvento<AsignacionReasignada>, AlReasignar>();
        servicios.AddScoped<IManejadorEvento<DesviacionEtaDetectada>, AlDesviarseLaEta>();
        servicios.AddScoped<IManejadorEvento<IncidenteReportado>, AlReportarIncidente>();
        servicios.AddScoped<IManejadorEvento<RecordatorioViajeDebido>, AlDebeRecordar>();
        servicios.AddScoped<IManejadorEvento<SinRecursoDisponible>, AlQuedarSinRecurso>();
        servicios.AddScoped<IManejadorEvento<SolicitudCancelada>, AlCancelarSolicitud>();
        servicios.AddScoped<IManejadorEvento<SolicitudRegistrada>, AlRegistrarSolicitud>();
        servicios.AddScoped<IManejadorEvento<ViajeFinalizado>, AlFinalizarViaje>();
        servicios.AddScoped<IManejadorEvento<ViajeIniciado>, AlIniciarViaje>();
        servicios.AddScoped<IMensajeEntranteRepository, MensajeEntranteRepository>();
        servicios.AddScoped<INotificacionEnviadaRepository, NotificacionEnviadaRepository>();
        servicios.AddScoped<IServicioAlertas, ServicioAlertas>();
        servicios.AddScoped<IServicioCargaItinerarios, ServicioCargaItinerarios>();
        servicios.AddScoped<IServicioConversaciones, ServicioConversaciones>();
        servicios.AddScoped<IServicioNotificaciones, ServicioNotificaciones>();
        servicios.AddScoped<IWhatsAppGateway, WhatsAppSandboxGateway>();

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
