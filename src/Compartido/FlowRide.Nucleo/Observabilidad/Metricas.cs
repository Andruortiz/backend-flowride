using System.Diagnostics.Metrics;

namespace FlowRide.Nucleo.Observabilidad;

public static class Metricas
{
    public const string Medidor = "FlowRide";

    private static readonly Meter Fuente = new(Medidor);

    public static Counter<long> MensajesEntrantes { get; } =
        Fuente.CreateCounter<long>("flowride.mensajes.entrantes", description: "Mensajes de WhatsApp recibidos por el bot, por resultado.");

    public static Counter<long> SolicitudesRegistradas { get; } =
        Fuente.CreateCounter<long>("flowride.solicitudes.registradas", description: "Solicitudes de transporte registradas.");

    public static Counter<long> AsignacionesAutomaticas { get; } =
        Fuente.CreateCounter<long>("flowride.asignaciones.automaticas", description: "Asignaciones automáticas, por resultado.");

    public static Counter<long> UbicacionesIngeridas { get; } =
        Fuente.CreateCounter<long>("flowride.ubicaciones.ingeridas", description: "Eventos de ubicación GPS procesados, por resultado.");

    public static Counter<long> OutboxFallos { get; } =
        Fuente.CreateCounter<long>("flowride.outbox.fallos", description: "Eventos del outbox cuyo despacho falló, por tipo.");

    public static KeyValuePair<string, object?> Etiqueta(string nombre, string valor) => new(nombre, valor);
}
