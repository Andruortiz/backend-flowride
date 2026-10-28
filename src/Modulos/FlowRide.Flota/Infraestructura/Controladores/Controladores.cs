using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Persistencia;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Flota.Infraestructura.Repositorios;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Flota.Infraestructura.Controladores;

[ApiController]
[Route("api/v1/conductores")]
public sealed class ConductoresController(IServicioFlota servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Administracion)]
    public async Task<ActionResult<ConductorResponse>> Crear(CrearConductorRequest request, CancellationToken ct)
    {
        var creado = await servicio.CrearConductorAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerConductorAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<ConductorResponse>> Listar(bool soloActivos = true, CancellationToken ct = default) =>
        await servicio.ListarConductoresAsync(soloActivos, ct);

    [HttpPut("{id:guid}/licencia")]
    [Authorize(Politicas.Administracion)]
    public async Task<ConductorResponse> RenovarLicencia(Guid id, DocumentoRequest request, CancellationToken ct) =>
        await servicio.RenovarLicenciaAsync(id, request, ct);

    [HttpPut("{id:guid}/turnos")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> DefinirTurnos(Guid id, DefinirTurnosRequest request, CancellationToken ct) =>
        await servicio.DefinirTurnosAsync(id, request, ct);

    [HttpPost("{id:guid}/no-disponible")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> NoDisponible(Guid id, MarcarNoDisponibleRequest request, CancellationToken ct) =>
        await servicio.MarcarNoDisponibleAsync(id, request, ct);

    [HttpPost("{id:guid}/disponible")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> Disponible(Guid id, CancellationToken ct) => await servicio.MarcarDisponibleAsync(id, ct);

    [HttpPost("{id:guid}/activar")]
    [Authorize(Politicas.Administracion)]
    public async Task<ConductorResponse> Activar(Guid id, CancellationToken ct) => await servicio.CambiarEstadoConductorAsync(id, true, ct);

    [HttpPost("{id:guid}/inactivar")]
    [Authorize(Politicas.Administracion)]
    public async Task<ConductorResponse> Inactivar(Guid id, CancellationToken ct) => await servicio.CambiarEstadoConductorAsync(id, false, ct);

    [HttpPut("{id:guid}/vehiculo")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> AsignarVehiculo(Guid id, AsignarVehiculoRequest request, CancellationToken ct) =>
        await servicio.AsignarVehiculoAsync(id, request, ct);

    [HttpDelete("{id:guid}/vehiculo")]
    [Authorize(Politicas.Gestion)]
    public async Task<ConductorResponse> QuitarVehiculo(Guid id, CancellationToken ct) => await servicio.QuitarVehiculoAsync(id, ct);
}

[ApiController]
[Route("api/v1/vehiculos")]
public sealed class VehiculosController(IServicioFlota servicio) : ControllerBase
{
    [HttpPost]
    [Authorize(Politicas.Administracion)]
    public async Task<ActionResult<VehiculoResponse>> Crear(CrearVehiculoRequest request, CancellationToken ct)
    {
        var creado = await servicio.CrearVehiculoAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.Gestion)]
    public async Task<VehiculoResponse> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerVehiculoAsync(id, ct);

    [HttpGet]
    [Authorize(Politicas.Gestion)]
    public async Task<IReadOnlyList<VehiculoResponse>> Listar(CancellationToken ct) => await servicio.ListarVehiculosAsync(ct);

    [HttpPut("{id:guid}/documentos")]
    [Authorize(Politicas.Administracion)]
    public async Task<VehiculoResponse> RegistrarDocumento(Guid id, DocumentoRequest request, CancellationToken ct) =>
        await servicio.RegistrarDocumentoVehiculoAsync(id, request, ct);

    [HttpPost("{id:guid}/habilitar")]
    [Authorize(Politicas.Administracion)]
    public async Task<VehiculoResponse> Habilitar(Guid id, CancellationToken ct) => await servicio.CambiarHabilitacionVehiculoAsync(id, true, ct);

    [HttpPost("{id:guid}/deshabilitar")]
    [Authorize(Politicas.Administracion)]
    public async Task<VehiculoResponse> Deshabilitar(Guid id, CancellationToken ct) => await servicio.CambiarHabilitacionVehiculoAsync(id, false, ct);
}
