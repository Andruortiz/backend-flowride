using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;

namespace FlowRide.Clientes.Aplicacion;

internal static class Mapeos
{
    public static ClienteEmpresarialResponse A(this ClienteEmpresarial c) => new(c.Id, c.Nombre, c.Nit, c.Activo);

    public static ItinerarioResponse A(this Itinerario i) =>
        new(i.Id, i.CodigoServicio, i.ClienteEmpresarialId, i.Origen, i.Destino, i.Fecha, i.HoraSalida, i.HoraLlegada);
}
