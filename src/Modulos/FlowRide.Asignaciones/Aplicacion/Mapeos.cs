using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;

namespace FlowRide.Asignaciones.Aplicacion;

internal static class Mapeos
{
    public static AsignacionResponse A(this Asignacion a) =>
        new(a.Id, a.SolicitudId, a.ConductorId, a.VehiculoId, a.Estado, a.EsManual, a.Motivo, a.VersionConfiguracion, a.Explicacion, a.CreadaEn, a.RespondidaEn);
}
