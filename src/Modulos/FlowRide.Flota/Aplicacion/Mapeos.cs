using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;

namespace FlowRide.Flota.Aplicacion;

internal static class Mapeos
{
    public static DocumentoResponse A(this DocumentoVigencia d, DateOnly hoy, int diasAviso) =>
        new(d.Tipo, d.Numero, d.VenceEl, d.EstadoEn(hoy, diasAviso));

    public static VehiculoResponse A(this Vehiculo v, DateOnly hoy, int diasAviso) =>
        new(v.Id, v.Placa, v.Capacidad, v.Habilitado, [.. v.Documentos.Select(d => d.A(hoy, diasAviso))]);
}
