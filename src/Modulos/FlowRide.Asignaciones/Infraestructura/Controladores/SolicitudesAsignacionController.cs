using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Nucleo.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowRide.Asignaciones.Infraestructura.Controladores;

/// <summary>Acciones de asignación sobre una solicitud. La ruta sigue siendo /solicitudes/{id}/… para no cambiar la API.</summary>
[ApiController]
[Route("api/v1/solicitudes")]
public sealed class SolicitudesAsignacionController(IServicioAsignaciones asignaciones) : ControllerBase
{
    [HttpPost("{id:guid}/asignar")]
    [Authorize(Politicas.Gestion)]
    public async Task<ActionResult<ResultadoAsignacionResponse>> Asignar(Guid id, CancellationToken ct)
    {
        var resultado = await asignaciones.AsignarAutomaticoAsync(id, ct);

        return resultado is null ? Conflict(new ProblemDetails { Title = "Conflicto", Detail = "La solicitud ya no está pendiente." }) : resultado;
    }

    [HttpPost("{id:guid}/reasignar")]
    [Authorize(Politicas.Gestion)]
    public async Task<ResultadoAsignacionResponse> ReasignarManual(Guid id, ReasignarManualRequest request, CancellationToken ct) =>
        await asignaciones.ReasignarManualAsync(id, request, ct);

    [HttpGet("{id:guid}/simulacion")]
    [Authorize(Politicas.Gestion)]
    public async Task<SimulacionAsignacionResponse> Simular(Guid id, CancellationToken ct) => await asignaciones.SimularAsync(id, ct);
}
