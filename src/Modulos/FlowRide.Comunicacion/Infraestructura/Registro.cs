using FlowRide.Comunicacion.Aplicacion;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Comunicacion.Infraestructura.Meta;
using FlowRide.Comunicacion.Infraestructura.Repositorios;
using Microsoft.Extensions.Configuration;
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
        servicios.AddHttpClient<ClienteMeta>(c => c.BaseAddress = new Uri("https://graph.facebook.com/"));
        servicios.AddScoped<IProcesadorWebhookMeta, ProcesadorWebhookMeta>();
        servicios.AddScoped<AdaptadorUbicacionWhatsApp>();
        servicios.AddScoped<FlowRide.Viajes.Aplicacion.Puertos.ITelemetryProviderAdapter>(sp => sp.GetRequiredService<AdaptadorUbicacionWhatsApp>());
        servicios.AddScoped<WhatsAppSandboxGateway>();
        servicios.AddScoped<IWhatsAppGateway>(sp =>
            string.IsNullOrWhiteSpace(sp.GetRequiredService<IConfiguration>()["WhatsApp:AccessToken"])
                ? sp.GetRequiredService<WhatsAppSandboxGateway>()
                : sp.GetRequiredService<ClienteMeta>());

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
