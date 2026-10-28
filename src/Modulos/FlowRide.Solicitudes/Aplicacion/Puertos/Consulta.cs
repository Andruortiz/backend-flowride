using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Solicitudes.Aplicacion.Puertos;

/// <summary>
/// Lo que este módulo deja leer a los demás. Las entidades devueltas siguen en el contexto de la solicitud:
/// si el módulo que consulta las modifica, se guardan en la misma transacción (único caso permitido, ver ADR de módulos).
/// </summary>
public interface IConsultaSolicitudes
{
    Task<SolicitudTransporte?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<SolicitudTransporte?> ObtenerUltimaDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudTransporte>> ListarParaRecordatorioAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudTransporte>> ListarAbiertasAntesDeAsync(DateTimeOffset limite, CancellationToken ct = default);
}
