using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Solicitudes.Aplicacion.Puertos;

public interface IConsultaSolicitudes
{
    Task<SolicitudTransporte?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<SolicitudTransporte?> ObtenerUltimaDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudTransporte>> ListarParaRecordatorioAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudTransporte>> ListarAbiertasAntesDeAsync(DateTimeOffset limite, DateTimeOffset serviciosHasta, CancellationToken ct = default);
    Task<IReadOnlyList<SolicitudTransporte>> ListarPendientesHastaAsync(DateTimeOffset serviciosHasta, CancellationToken ct = default);
}
