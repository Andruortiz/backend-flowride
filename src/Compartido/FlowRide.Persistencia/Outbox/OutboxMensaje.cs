namespace FlowRide.Persistencia.Outbox;

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
