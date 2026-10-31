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

internal interface IViajeRepository
{
    Task<Viaje?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Viaje?> ObtenerPorSolicitudAsync(Guid solicitudId, CancellationToken ct = default);

    Task<Viaje?> ObtenerPorAsignacionAsync(Guid asignacionId, CancellationToken ct = default);

    Task<Viaje?> ObtenerEnCursoDeVehiculoAsync(Guid vehiculoId, CancellationToken ct = default);

    Task<Viaje?> ObtenerVigenteDeConductorAsync(Guid conductorId, CancellationToken ct = default);

    Task<Viaje?> ObtenerUltimoFinalizadoDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);

    Task<IReadOnlyList<Viaje>> ListarAsync(EstadoViaje? estado, Guid? conductorId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default);

    Task AgregarAsync(Viaje viaje, CancellationToken ct = default);
}

internal interface ICalificacionRepository
{
    Task<bool> ExistePorViajeAsync(Guid viajeId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, double>> PromedioPorConductorAsync(IEnumerable<Guid> conductorIds, CancellationToken ct = default);

    Task AgregarAsync(Calificacion calificacion, CancellationToken ct = default);
}

internal interface IUbicacionRepository
{
    Task<bool> ExisteAsync(Guid vehiculoId, DateTimeOffset timestampOrigen, string fuente, CancellationToken ct = default);

    Task<IReadOnlyList<RegistroUbicacion>> ObtenerRecientesAsync(Guid vehiculoId, DateTimeOffset desde, int maximo, CancellationToken ct = default);

    Task AgregarAsync(RegistroUbicacion registro, CancellationToken ct = default);
}
