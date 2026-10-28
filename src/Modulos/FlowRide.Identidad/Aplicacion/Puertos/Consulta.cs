using FlowRide.Identidad.Dominio;

namespace FlowRide.Identidad.Aplicacion.Puertos;

/// <summary>
/// Lo que este módulo deja leer a los demás. Las entidades devueltas siguen en el contexto de la solicitud:
/// si el módulo que consulta las modifica, se guardan en la misma transacción (único caso permitido, ver ADR de módulos).
/// </summary>
public interface IConsultaIdentidad
{
    Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObtenerUsuarioPorTelefonoAsync(string telefono, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerUsuariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Pasajero?> ObtenerPasajeroAsync(Guid id, CancellationToken ct = default);
    Task<Pasajero?> ObtenerPasajeroPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);
}
