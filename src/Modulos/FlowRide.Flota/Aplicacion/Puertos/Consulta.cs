using FlowRide.Flota.Dominio;

namespace FlowRide.Flota.Aplicacion.Puertos;

public interface IConsultaFlota
{
    Task<Conductor?> ObtenerConductorAsync(Guid id, CancellationToken ct = default);
    Task<Conductor?> ObtenerConductorPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<Conductor>> ListarConductoresAsync(bool soloActivos, CancellationToken ct = default);
    Task<Vehiculo?> ObtenerVehiculoAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Vehiculo>> ObtenerVehiculosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Vehiculo>> ListarVehiculosAsync(CancellationToken ct = default);
}
