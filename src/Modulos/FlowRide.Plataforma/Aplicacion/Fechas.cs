namespace FlowRide.Plataforma.Aplicacion;

public static class Fechas
{
    public static DateOnly Hoy(TimeProvider reloj, TimeZoneInfo zona) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), zona).DateTime);
}
