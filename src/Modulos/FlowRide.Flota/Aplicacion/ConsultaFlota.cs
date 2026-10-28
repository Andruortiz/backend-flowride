using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;

namespace FlowRide.Flota.Aplicacion;

internal sealed class ConsultaFlota(IConductorRepository conductores, IVehiculoRepository vehiculos) : IConsultaFlota
{
    public Task<Conductor?> ObtenerConductorAsync(Guid id, CancellationToken ct = default) =>
        conductores.ObtenerAsync(id, ct);

    public Task<Conductor?> ObtenerConductorPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        conductores.ObtenerPorUsuarioAsync(usuarioId, ct);

    public Task<IReadOnlyList<Conductor>> ListarConductoresAsync(bool soloActivos, CancellationToken ct = default) =>
        conductores.ListarAsync(soloActivos, ct);

    public Task<Vehiculo?> ObtenerVehiculoAsync(Guid id, CancellationToken ct = default) =>
        vehiculos.ObtenerAsync(id, ct);

    public Task<IReadOnlyDictionary<Guid, Vehiculo>> ObtenerVehiculosAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        vehiculos.ObtenerVariosAsync(ids, ct);

    public Task<IReadOnlyList<Vehiculo>> ListarVehiculosAsync(CancellationToken ct = default) =>
        vehiculos.ListarAsync(ct);
}
