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

internal interface IConductorRepository
{
    Task<Conductor?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Conductor?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, Conductor>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Conductor>> ListarAsync(bool soloActivos, CancellationToken ct = default);

    Task AgregarAsync(Conductor conductor, CancellationToken ct = default);
}

internal interface IVehiculoRepository
{
    Task<Vehiculo?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Vehiculo?> ObtenerPorPlacaAsync(string placa, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, Vehiculo>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Vehiculo>> ListarAsync(CancellationToken ct = default);

    Task AgregarAsync(Vehiculo vehiculo, CancellationToken ct = default);
}
