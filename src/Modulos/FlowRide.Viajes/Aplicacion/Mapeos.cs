using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;

namespace FlowRide.Viajes.Aplicacion;

internal static class Mapeos
{
    public static ViajeResponse A(this Viaje v) =>
        new(v.Id, v.SolicitudId, v.AsignacionId, v.PasajeroId, v.ConductorId, v.VehiculoId, v.Estado, v.ProgramadoPara, v.EtaProgramada,
            v.IniciadoEn, v.FinalizadoEn, v.AlertaEtaActiva, [.. v.Hitos.Select(h => new HitoResponse(h.Instante, h.Tipo, h.Detalle))]);

    public static CalificacionResponse A(this Calificacion c) => new(c.Id, c.ViajeId, c.Puntaje, c.Comentario, c.CreadaEn);
}
