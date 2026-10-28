using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;

namespace FlowRide.Comunicacion.Aplicacion;

internal static class Mapeos
{
    public static AlertaResponse A(this AlertaOperativa a) =>
        new(a.Id, a.Tipo, a.Severidad, a.Titulo, a.Detalle, a.Referencia, a.CreadaEn, a.LeidaEn);
}
