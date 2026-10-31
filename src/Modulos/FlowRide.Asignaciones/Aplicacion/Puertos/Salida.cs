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

internal interface IAsignacionRepository
{
    Task<Asignacion?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Asignacion?> ObtenerActivaPorSolicitudAsync(Guid solicitudId, CancellationToken ct = default);

    Task<Asignacion?> ObtenerPendienteDeConductorAsync(Guid conductorId, CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> ConductoresDescartadosAsync(Guid solicitudId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> ContarActivasPorConductorAsync(IEnumerable<Guid> conductorIds, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> ContarRecientesPorConductorAsync(IEnumerable<Guid> conductorIds, DateTimeOffset desde, CancellationToken ct = default);

    Task<IReadOnlyList<Asignacion>> ListarAsync(Guid? conductorId, EstadoAsignacion? estado, int pagina, int tamano, CancellationToken ct = default);

    Task AgregarAsync(Asignacion asignacion, CancellationToken ct = default);
}

internal interface IConfiguracionAsignacionRepository
{
    Task<ConfiguracionAsignacion?> ObtenerActivaAsync(CancellationToken ct = default);

    Task AgregarAsync(ConfiguracionAsignacion configuracion, CancellationToken ct = default);
}
