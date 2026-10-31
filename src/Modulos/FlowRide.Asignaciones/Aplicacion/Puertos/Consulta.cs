using FlowRide.Asignaciones.Dominio;

namespace FlowRide.Asignaciones.Aplicacion.Puertos;

public interface IConsultaAsignaciones
{
    Task<Asignacion?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<Asignacion?> ObtenerPendienteDeConductorAsync(Guid conductorId, CancellationToken ct = default);
}
