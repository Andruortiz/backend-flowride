using FlowRide.Identidad.Dominio;

namespace FlowRide.Identidad.Aplicacion.Puertos;

public interface IConsultaIdentidad
{
    Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObtenerUsuarioPorTelefonoAsync(string telefono, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Pasajero?> ObtenerPasajeroAsync(Guid id, CancellationToken ct = default);
    Task<Pasajero?> ObtenerPasajeroPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
