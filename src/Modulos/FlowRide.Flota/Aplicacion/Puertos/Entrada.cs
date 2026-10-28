using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Flota.Aplicacion.Puertos;

public interface IServicioFlota
{
    Task<ConductorResponse> CrearConductorAsync(CrearConductorRequest request, CancellationToken ct = default);

    Task<ConductorResponse> ObtenerConductorAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ConductorResponse>> ListarConductoresAsync(bool soloActivos, CancellationToken ct = default);

    Task<ConductorResponse> RenovarLicenciaAsync(Guid id, DocumentoRequest request, CancellationToken ct = default);

    Task<ConductorResponse> DefinirTurnosAsync(Guid id, DefinirTurnosRequest request, CancellationToken ct = default);

    Task<ConductorResponse> MarcarNoDisponibleAsync(Guid id, MarcarNoDisponibleRequest request, CancellationToken ct = default);

    Task<ConductorResponse> MarcarDisponibleAsync(Guid id, CancellationToken ct = default);

    Task<ConductorResponse> CambiarEstadoConductorAsync(Guid id, bool activo, CancellationToken ct = default);

    Task<ConductorResponse> AsignarVehiculoAsync(Guid id, AsignarVehiculoRequest request, CancellationToken ct = default);

    Task<ConductorResponse> QuitarVehiculoAsync(Guid id, CancellationToken ct = default);

    Task<VehiculoResponse> CrearVehiculoAsync(CrearVehiculoRequest request, CancellationToken ct = default);

    Task<VehiculoResponse> ObtenerVehiculoAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<VehiculoResponse>> ListarVehiculosAsync(CancellationToken ct = default);

    Task<VehiculoResponse> RegistrarDocumentoVehiculoAsync(Guid id, DocumentoRequest request, CancellationToken ct = default);

    Task<VehiculoResponse> CambiarHabilitacionVehiculoAsync(Guid id, bool habilitado, CancellationToken ct = default);
}
