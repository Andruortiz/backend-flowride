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
    EsperandoDomicilio = 8,
    EsperandoArchivo = 9,
    EsperandoConfirmacionCarga = 10,
}

public enum IntencionConversacion
{
    Ninguna = 0,
    SolicitudCompleta = 1,
    ConsultarEstado = 2,
}

public sealed record DatosSolicitudConversacion(string Origen, string Destino, DateTimeOffset FechaHoraUtc, bool Urgente);

public sealed record RespuestaConversacion(string Clave, IntencionConversacion Intencion, DatosSolicitudConversacion? Solicitud = null, IReadOnlyDictionary<string, string>? Variables = null);

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

    public string? ArchivoItinerario { get; private set; }

    public bool EnCarga => Estado is EstadoConversacion.EsperandoDomicilio or EstadoConversacion.EsperandoArchivo or EstadoConversacion.EsperandoConfirmacionCarga;

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
            return Responder("bot.cancelada");
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

    private static RespuestaConversacion Responder(string clave) => new(clave, IntencionConversacion.Ninguna);

    private RespuestaConversacion Empezar(string comando)
    {
        if (comando is "solicitar" or "nuevo" or "hola" or "1")
        {
            Reiniciar();
            Estado = EstadoConversacion.EsperandoOrigen;

            return Responder("bot.origen.pedir");
        }

        return Responder("bot.saludo");
    }

    private RespuestaConversacion PedirOrigen(string mensaje)
    {
        if (TextHelper.IsEmptyWithTrim(mensaje))
        {
            return Responder("bot.origen.invalido");
        }

        Origen = mensaje;
        Estado = EstadoConversacion.EsperandoDestino;

        return Responder("bot.destino.pedir");
    }

    private RespuestaConversacion PedirDestino(string mensaje)
    {
        if (TextHelper.IsEmptyWithTrim(mensaje) || string.Equals(mensaje, Origen, StringComparison.OrdinalIgnoreCase))
        {
            return Responder("bot.destino.invalido");
        }

        Destino = mensaje;
        Estado = EstadoConversacion.EsperandoFechaHora;

        return Responder("bot.fecha.pedir");
    }

    private RespuestaConversacion PedirFechaHora(string mensaje, DateTimeOffset ahora, TimeZoneInfo zona)
    {
        if (!DateTime.TryParseExact(mensaje, FormatosFechaHora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return Responder("bot.fecha.formato");
        }

        DateTimeOffset utc;
        try
        {
            utc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona), TimeSpan.Zero);
        }
        catch (ArgumentException)
        {
            return Responder("bot.fecha.inexistente");
        }

        if (utc <= ahora)
        {
            return Responder("bot.fecha.pasada");
        }

        FechaHoraUtc = utc;
        Estado = EstadoConversacion.EsperandoUrgencia;

        return Responder("bot.urgencia.pedir");
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
            return Responder("bot.urgencia.invalida");
        }

        Estado = EstadoConversacion.EsperandoConfirmacion;
        var cuando = TimeZoneInfo.ConvertTime(FechaHoraUtc!.Value, zona).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return new RespuestaConversacion(
            Urgente == true ? "bot.confirmar.resumen_urgente" : "bot.confirmar.resumen",
            IntencionConversacion.Ninguna,
            Variables: new Dictionary<string, string> { ["origen"] = Origen!, ["destino"] = Destino!, ["cuando"] = cuando });
    }

    private RespuestaConversacion Confirmar(string comando)
    {
        if (Array.IndexOf(Afirmativos, comando) < 0)
        {
            return Responder("bot.confirmar.invalido");
        }

        var datos = new DatosSolicitudConversacion(Origen!, Destino!, FechaHoraUtc!.Value, Urgente == true);
        Estado = EstadoConversacion.Completa;

        return new RespuestaConversacion("bot.confirmar.listo", IntencionConversacion.SolicitudCompleta, datos);
    }

    public void EsperarDomicilio(DateTimeOffset ahora) => Avanzar(EstadoConversacion.EsperandoDomicilio, null, ahora);

    public void EsperarArchivo(DateTimeOffset ahora) => Avanzar(EstadoConversacion.EsperandoArchivo, null, ahora);

    public void EsperarConfirmacionCarga(string archivo, DateTimeOffset ahora) =>
        Avanzar(EstadoConversacion.EsperandoConfirmacionCarga, archivo, ahora);

    public void Terminar(DateTimeOffset ahora) => Avanzar(EstadoConversacion.Inicio, null, ahora);

    private void Avanzar(EstadoConversacion estado, string? archivo, DateTimeOffset ahora)
    {
        Reiniciar();
        Estado = estado;
        ArchivoItinerario = archivo;
        ActualizadaEn = ahora;
        ExpiraEn = ahora + Vigencia;
    }

    private void Reiniciar()
    {
        Estado = EstadoConversacion.Inicio;
        Origen = null;
        Destino = null;
        FechaHoraUtc = null;
        Urgente = null;
        ArchivoItinerario = null;
    }
}
