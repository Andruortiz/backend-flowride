using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowRide.Asignaciones.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/solicitudes")]
public sealed class SolicitudesAsignacionController(IServicioAsignaciones asignaciones) : ControllerBase
{
    [HttpPost("{id:guid}/asignar")]
    [Authorize(Politicas.Gestion)]
    public async Task<ActionResult<ResultadoAsignacionResponse>> Asignar(Guid id, CancellationToken ct)
    {
        var resultado = await asignaciones.AsignarAutomaticoAsync(id, ct);

        return resultado ?? throw new ConflictException("error.la_solicitud_ya_no_esta_pendiente");
    }

    [HttpPost("{id:guid}/reasignar")]
    [Authorize(Politicas.Gestion)]
    public async Task<ResultadoAsignacionResponse> ReasignarManual(Guid id, ReasignarManualRequest request, CancellationToken ct) =>
        await asignaciones.ReasignarManualAsync(id, request, ct);

    [HttpGet("{id:guid}/simulacion")]
    [Authorize(Politicas.Gestion)]
    public async Task<SimulacionAsignacionResponse> Simular(Guid id, CancellationToken ct) => await asignaciones.SimularAsync(id, ct);
}
