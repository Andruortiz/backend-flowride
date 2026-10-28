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

internal interface IConversacionRepository
{
    Task<ConversacionBot?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task AgregarAsync(ConversacionBot conversacion, CancellationToken ct = default);
}

/// <summary>Idempotencia de los mensajes entrantes: cada mensaje externo se procesa una sola vez.</summary>
internal interface IMensajeEntranteRepository
{
    Task<bool> ExisteAsync(string externalMessageId, CancellationToken ct = default);

    Task RegistrarAsync(string externalMessageId, DateTimeOffset recibidoEn, CancellationToken ct = default);
}

/// <summary>Registro de lo ya enviado por cada canal: un reintento no genera un mensaje duplicado.</summary>
internal interface INotificacionEnviadaRepository
{
    Task<bool> ExisteAsync(Guid eventoId, Guid destinatarioId, CanalNotificacion canal, CancellationToken ct = default);

    Task RegistrarAsync(Guid eventoId, Guid destinatarioId, CanalNotificacion canal, string clave, DateTimeOffset enviadaEn, CancellationToken ct = default);
}

internal interface IAlertaRepository
{
    Task<AlertaOperativa?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteClaveAsync(string claveDeduplicacion, CancellationToken ct = default);

    Task<IReadOnlyList<AlertaOperativa>> ListarAsync(bool soloNoLeidas, int pagina, int tamano, CancellationToken ct = default);

    Task<int> ContarNoLeidasAsync(CancellationToken ct = default);

    Task AgregarAsync(AlertaOperativa alerta, CancellationToken ct = default);
}

/// <summary>Salida hacia WhatsApp. El proveedor concreto (Sandbox, Twilio, Meta) se conecta detrás de este puerto (ADR-0008).</summary>
internal interface IWhatsAppGateway
{
    Task EnviarAsync(string telefono, string texto, CancellationToken ct = default);
}
