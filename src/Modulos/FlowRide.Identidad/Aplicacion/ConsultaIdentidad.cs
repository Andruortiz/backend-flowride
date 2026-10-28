using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;

namespace FlowRide.Identidad.Aplicacion;

internal sealed class ConsultaIdentidad(IUsuarioRepository usuarios, IPasajeroRepository pasajeros) : IConsultaIdentidad
{
    public Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken ct = default) =>
        usuarios.ObtenerAsync(id, ct);

    public Task<Usuario?> ObtenerUsuarioPorTelefonoAsync(string telefono, CancellationToken ct = default) =>
        usuarios.ObtenerPorTelefonoAsync(telefono, ct);

    public Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        usuarios.ObtenerVariosAsync(ids, ct);

    public Task<Pasajero?> ObtenerPasajeroAsync(Guid id, CancellationToken ct = default) =>
        pasajeros.ObtenerAsync(id, ct);

    public Task<Pasajero?> ObtenerPasajeroPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        pasajeros.ObtenerPorUsuarioAsync(usuarioId, ct);
}
