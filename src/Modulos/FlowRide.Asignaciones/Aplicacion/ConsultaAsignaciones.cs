using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;

namespace FlowRide.Asignaciones.Aplicacion;

internal sealed class ConsultaAsignaciones(IAsignacionRepository asignaciones) : IConsultaAsignaciones
{
    public Task<Asignacion?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        asignaciones.ObtenerAsync(id, ct);

    public Task<Asignacion?> ObtenerPendienteDeConductorAsync(Guid conductorId, CancellationToken ct = default) =>
        asignaciones.ObtenerPendienteDeConductorAsync(conductorId, ct);
}
