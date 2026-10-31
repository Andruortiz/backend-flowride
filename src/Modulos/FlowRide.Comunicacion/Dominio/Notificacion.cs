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

public static class CatalogoMensajes
{
    private static readonly Dictionary<string, string> Textos = new()
    {
        ["solicitud.registrada"] = "Recibimos tu solicitud de {origen} a {destino} para el {fecha}. Te avisaremos cuando tengamos conductor.",
        ["asignacion.pasajero"] = "Tu servicio del {fecha} tiene conductor: {conductor}, vehículo {placa}.",
        ["asignacion.conductor"] = "Nuevo servicio: {pasajero}, de {origen} a {destino}, el {fecha}. Responde ACEPTAR o RECHAZAR.",
        ["sin_conductor.pasajero"] = "Por ahora no hay conductor disponible para tu servicio del {fecha}. Un operador lo revisará.",
        ["solicitud_cancelada.conductor"] = "Se canceló el servicio del {fecha} ({pasajero}).",
        ["reasignacion.conductor_anterior"] = "El servicio del {fecha} ya no está a tu cargo.",
        ["viaje_iniciado.pasajero"] = "Tu viaje inició. Conductor: {conductor}.",
        ["viaje_finalizado.pasajero"] = "Tu viaje terminó. Puedes calificarlo de 1 a 5 respondiendo CALIFICAR y el número.",
        ["recordatorio.viaje"] = "Recordatorio: tienes un servicio el {fecha}.",
        ["eta_desviacion.pasajero"] = "Tu servicio llegará con aproximadamente {minutos} minutos de retraso.",
    };

    public static string Renderizar(string clave, IReadOnlyDictionary<string, string>? variables = null)
    {
        if (!Textos.TryGetValue(clave, out var texto))
        {
            throw new KeyNotFoundException($"No existe el mensaje '{clave}'.");
        }

        if (variables is null)
        {
            return texto;
        }

        foreach (var (nombre, valor) in variables)
        {
            texto = texto.Replace("{" + nombre + "}", valor, StringComparison.Ordinal);
        }

        return texto;
    }
}
