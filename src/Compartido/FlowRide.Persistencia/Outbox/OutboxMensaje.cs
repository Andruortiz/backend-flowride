namespace FlowRide.Persistencia.Outbox;

/// <summary>Evento de dominio pendiente de publicar. Se escribe en la misma transacción que el cambio que lo originó (ADR-0007).</summary>
public sealed class OutboxMensaje
{
    public Guid Id { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Carga { get; set; } = string.Empty;

    public DateTimeOffset OcurrioEn { get; set; }

    public DateTimeOffset DisponibleDesde { get; set; }

    public DateTimeOffset? ProcesadoEn { get; set; }

    public int Intentos { get; set; }

    public string? UltimoError { get; set; }
}
