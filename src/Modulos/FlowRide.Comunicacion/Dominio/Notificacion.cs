using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Viajes.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Comunicacion.Dominio;

public enum CanalNotificacion
{
    WhatsApp = 1,
    Panel = 2,
    Correo = 3,
}

public enum TipoNotificacion
{
    SolicitudRegistrada = 1,
    AsignacionPasajero = 2,
    AsignacionConductor = 3,
    SinConductorPasajero = 4,
    SolicitudCanceladaConductor = 5,
    ReasignacionConductorAnterior = 6,
    ViajeIniciado = 7,
    ViajeFinalizado = 8,
    Recordatorio = 9,
    DesviacionEtaPasajero = 10,
}

public sealed record SolicitudNotificacion(
    Guid EventoId,
    Guid DestinatarioId,
    RolUsuario Rol,
    TipoNotificacion Tipo,
    string ClaveMensaje,
    IReadOnlyDictionary<string, string> Variables);

public static class PoliticaCanal
{
    public static IReadOnlyList<CanalNotificacion> Resolver(RolUsuario rol, bool critica = false) =>
        rol is RolUsuario.Pasajero or RolUsuario.Conductor
            ? [CanalNotificacion.WhatsApp]
            : critica ? [CanalNotificacion.Panel, CanalNotificacion.Correo] : [CanalNotificacion.Panel];
}
