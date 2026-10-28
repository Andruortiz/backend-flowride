using System.Globalization;
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

public enum EstadoConversacion
{
    Inicio = 1,
    EsperandoOrigen = 2,
    EsperandoDestino = 3,
    EsperandoFechaHora = 4,
    EsperandoUrgencia = 5,
    EsperandoConfirmacion = 6,
    Completa = 7,
}

public enum IntencionConversacion
{
    Ninguna = 0,
    SolicitudCompleta = 1,
    ConsultarEstado = 2,
}

public sealed record DatosSolicitudConversacion(string Origen, string Destino, DateTimeOffset FechaHoraUtc, bool Urgente);

public sealed record RespuestaConversacion(string Texto, IntencionConversacion Intencion, DatosSolicitudConversacion? Solicitud = null);

/// <summary>
/// Máquina de estados del bot de WhatsApp para pedir un servicio (RF-15). Es independiente del proveedor del canal:
/// recibe texto y devuelve la respuesta y, al completarse, los datos de la solicitud.
/// </summary>
public sealed class ConversacionBot : Entidad
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(30);

    private static readonly string[] FormatosFechaHora = ["yyyy-MM-dd HH:mm", "dd/MM/yyyy HH:mm"];
    private static readonly string[] Afirmativos = ["si", "sí", "s", "urgente", "confirmar", "ok"];
    private static readonly string[] Negativos = ["no", "n"];

    private ConversacionBot(Guid id, Guid usuarioId, DateTimeOffset actualizadaEn)
        : base(id)
    {
        UsuarioId = usuarioId;
        Estado = EstadoConversacion.Inicio;
        ActualizadaEn = actualizadaEn;
        ExpiraEn = actualizadaEn + Vigencia;
    }

    public Guid UsuarioId { get; }

    public EstadoConversacion Estado { get; private set; }

    public string? Origen { get; private set; }

    public string? Destino { get; private set; }

    public DateTimeOffset? FechaHoraUtc { get; private set; }

    public bool? Urgente { get; private set; }

    public DateTimeOffset ActualizadaEn { get; private set; }

    public DateTimeOffset ExpiraEn { get; private set; }

    public static ConversacionBot Iniciar(Guid usuarioId, DateTimeOffset ahora, Guid? id = null) =>
        new(UuidHelper.GetOrGenerate(id), usuarioId, ahora);

    public RespuestaConversacion Procesar(string texto, DateTimeOffset ahora, TimeZoneInfo zona)
    {
        ArgumentNullException.ThrowIfNull(zona);

        var mensaje = TextHelper.GetDefaultWithTrim(texto);
        var comando = mensaje.ToLowerInvariant();

        if (ahora > ExpiraEn)
        {
            Reiniciar();
        }

        ActualizadaEn = ahora;
        ExpiraEn = ahora + Vigencia;

        if (comando == "estado")
        {
            return new RespuestaConversacion(string.Empty, IntencionConversacion.ConsultarEstado);
        }

        if (comando == "cancelar")
        {
            Reiniciar();
            return Responder("Cancelé la solicitud en curso. Escribe SOLICITAR para empezar de nuevo.");
        }

        return Estado switch
        {
            EstadoConversacion.Inicio or EstadoConversacion.Completa => Empezar(comando),
            EstadoConversacion.EsperandoOrigen => PedirOrigen(mensaje),
            EstadoConversacion.EsperandoDestino => PedirDestino(mensaje),
            EstadoConversacion.EsperandoFechaHora => PedirFechaHora(mensaje, ahora, zona),
            EstadoConversacion.EsperandoUrgencia => PedirUrgencia(comando, zona),
            _ => Confirmar(comando),
        };
    }

    private static RespuestaConversacion Responder(string texto) => new(texto, IntencionConversacion.Ninguna);

    private RespuestaConversacion Empezar(string comando)
    {
        if (comando is "solicitar" or "nuevo" or "hola" or "1")
        {
            Reiniciar();
            Estado = EstadoConversacion.EsperandoOrigen;

            return Responder("¿Desde dónde sales? Escribe la dirección o el punto de origen.");
        }

        return Responder("Hola. Escribe SOLICITAR para pedir un servicio o ESTADO para ver tu última solicitud.");
    }

    private RespuestaConversacion PedirOrigen(string mensaje)
    {
        if (TextHelper.IsEmptyWithTrim(mensaje))
        {
            return Responder("No entendí el origen. ¿Desde dónde sales?");
        }

        Origen = mensaje;
        Estado = EstadoConversacion.EsperandoDestino;

        return Responder("¿A dónde vas?");
    }

    private RespuestaConversacion PedirDestino(string mensaje)
    {
        if (TextHelper.IsEmptyWithTrim(mensaje) || string.Equals(mensaje, Origen, StringComparison.OrdinalIgnoreCase))
        {
            return Responder("El destino debe ser distinto del origen. ¿A dónde vas?");
        }

        Destino = mensaje;
        Estado = EstadoConversacion.EsperandoFechaHora;

        return Responder("¿Cuándo? Escribe la fecha y hora local así: 2026-10-14 08:30");
    }

    private RespuestaConversacion PedirFechaHora(string mensaje, DateTimeOffset ahora, TimeZoneInfo zona)
    {
        if (!DateTime.TryParseExact(mensaje, FormatosFechaHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return Responder("No entendí la fecha. Usa este formato: 2026-10-14 08:30");
        }

        DateTimeOffset utc;
        try
        {
            utc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona), TimeSpan.Zero);
        }
        catch (ArgumentException)
        {
            return Responder("Esa hora no existe en tu zona horaria. Escribe otra.");
        }

        if (utc <= ahora)
        {
            return Responder("Esa fecha y hora ya pasó. Escribe una futura.");
        }

        FechaHoraUtc = utc;
        Estado = EstadoConversacion.EsperandoUrgencia;

        return Responder("¿Es urgente? Responde SÍ o NO.");
    }

    private RespuestaConversacion PedirUrgencia(string comando, TimeZoneInfo zona)
    {
        if (Array.IndexOf(Afirmativos, comando) >= 0)
        {
            Urgente = true;
        }
        else if (Array.IndexOf(Negativos, comando) >= 0)
        {
            Urgente = false;
        }
        else
        {
            return Responder("Responde SÍ o NO. ¿Es urgente?");
        }

        Estado = EstadoConversacion.EsperandoConfirmacion;
        var cuando = TimeZoneInfo.ConvertTime(FechaHoraUtc!.Value, zona).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return Responder(
            $"Confirma tu solicitud: de {Origen} a {Destino}, el {cuando}{(Urgente == true ? " (urgente)" : string.Empty)}. Responde CONFIRMAR o CANCELAR.");
    }

    private RespuestaConversacion Confirmar(string comando)
    {
        if (Array.IndexOf(Afirmativos, comando) < 0)
        {
            return Responder("Responde CONFIRMAR para registrar la solicitud o CANCELAR para descartarla.");
        }

        var datos = new DatosSolicitudConversacion(Origen!, Destino!, FechaHoraUtc!.Value, Urgente == true);
        Estado = EstadoConversacion.Completa;

        return new RespuestaConversacion("Listo, registré tu solicitud. Te avisaremos cuando tengamos conductor.", IntencionConversacion.SolicitudCompleta, datos);
    }

    private void Reiniciar()
    {
        Estado = EstadoConversacion.Inicio;
        Origen = null;
        Destino = null;
        FechaHoraUtc = null;
        Urgente = null;
    }
}
