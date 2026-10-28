using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Viajes.Aplicacion.Puertos;

public interface IServicioViajes
{
    Task<ViajeResponse> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ViajeResponse>> ListarAsync(
        EstadoViaje? estado, Guid? conductorId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default);

    Task<ViajeResponse> IniciarAsync(Guid id, CancellationToken ct = default);

    Task<ViajeResponse> FinalizarAsync(Guid id, CancellationToken ct = default);

    Task<ViajeResponse> ReportarIncidenteAsync(Guid id, ReportarIncidenteRequest request, CancellationToken ct = default);

    Task<CalificacionResponse> CalificarAsync(Guid id, CalificarViajeRequest request, CancellationToken ct = default);
}

public interface IServicioSeguimiento
{
    /// <summary>Ingesta de un evento GPS. Duplicado (vehículo, instante, fuente) se acepta sin volver a guardarlo.</summary>
    Task<UbicacionResponse> ReportarAsync(ReportarUbicacionRequest request, CancellationToken ct = default);
}
