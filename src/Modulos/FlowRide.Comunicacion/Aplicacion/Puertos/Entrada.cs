using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Aplicacion.Puertos;

public interface IServicioNotificaciones
{
    /// <summary>Envía por el canal que corresponde al destinatario. Un reintento del mismo evento no repite el mensaje.</summary>
    Task EnviarAUsuarioAsync(Guid usuarioId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default);

    Task EnviarAPasajeroAsync(Guid pasajeroId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default);

    Task EnviarAConductorAsync(Guid conductorId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default);
}

public interface IServicioAlertas
{
    /// <summary>Crea la alerta salvo que ya exista una con la misma clave (evita repetirla en cada reintento o evento GPS).</summary>
    Task CrearAsync(string tipo, SeveridadAlerta severidad, string titulo, string detalle, string referencia, string claveDeduplicacion, CancellationToken ct = default);

    Task<IReadOnlyList<AlertaResponse>> ListarAsync(bool soloNoLeidas, int pagina, int tamano, CancellationToken ct = default);

    Task<int> ContarNoLeidasAsync(CancellationToken ct = default);

    Task MarcarLeidaAsync(Guid id, CancellationToken ct = default);
}

public interface IServicioConversaciones
{
    /// <summary>
    /// Procesa un mensaje entrante de WhatsApp: identifica al usuario por su teléfono, ejecuta lo que pidió y responde.
    /// Un mensaje con el mismo identificador externo se procesa una sola vez.
    /// </summary>
    Task<RespuestaBotResponse> ProcesarAsync(MensajeEntranteRequest request, CancellationToken ct = default);
}
