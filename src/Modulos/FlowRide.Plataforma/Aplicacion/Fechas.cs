namespace FlowRide.Plataforma.Aplicacion;

/// <summary>Las fechas de negocio se interpretan en la zona del operador; los instantes siguen en UTC.</summary>
public static class Fechas
{
    public static DateOnly Hoy(TimeProvider reloj, TimeZoneInfo zona) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zona).DateTime);
}
