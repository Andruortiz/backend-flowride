using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Aplicacion.Puertos;

public interface IServicioAsignaciones
{
    Task<ResultadoAsignacionResponse?> AsignarAutomaticoAsync(Guid solicitudId, CancellationToken ct = default);

    Task<ResultadoAsignacionResponse> ReasignarManualAsync(Guid solicitudId, ReasignarManualRequest request, CancellationToken ct = default);

    Task<AsignacionResponse> AceptarAsync(Guid asignacionId, CancellationToken ct = default);

    Task<AsignacionResponse> RechazarAsync(Guid asignacionId, CancellationToken ct = default);

    Task<SimulacionAsignacionResponse> SimularAsync(Guid solicitudId, CancellationToken ct = default);

    Task<AsignacionResponse> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AsignacionResponse>> ListarAsync(Guid? conductorId, EstadoAsignacion? estado, int pagina, int tamano, CancellationToken ct = default);

    Task<ConfiguracionAsignacionResponse> ObtenerConfiguracionAsync(CancellationToken ct = default);

    Task<ConfiguracionAsignacionResponse> ActualizarConfiguracionAsync(ActualizarConfiguracionAsignacionRequest request, CancellationToken ct = default);
}
