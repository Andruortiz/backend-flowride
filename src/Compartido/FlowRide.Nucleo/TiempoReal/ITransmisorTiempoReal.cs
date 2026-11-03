namespace FlowRide.Nucleo.TiempoReal;

public interface ITransmisorTiempoReal
{
    Task PublicarAsync(string tema, CancellationToken ct = default);
}

public static class TemasTiempoReal
{
    public const string Solicitudes = "solicitudes";
    public const string Asignaciones = "asignaciones";
    public const string Viajes = "viajes";
    public const string Alertas = "alertas";
    public const string Ubicaciones = "ubicaciones";
}
