using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Solicitudes.Aplicacion;

internal sealed class ConsultaSolicitudes(ISolicitudRepository solicitudes) : IConsultaSolicitudes
{
    public Task<SolicitudTransporte?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        solicitudes.ObtenerAsync(id, ct);

    public Task<SolicitudTransporte?> ObtenerUltimaDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default) =>
        solicitudes.ObtenerUltimaDePasajeroAsync(pasajeroId, ct);

    public Task<IReadOnlyList<SolicitudTransporte>> ListarParaRecordatorioAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        solicitudes.ListarParaRecordatorioAsync(desde, hasta, ct);

    public Task<IReadOnlyList<SolicitudTransporte>> ListarAbiertasAntesDeAsync(DateTimeOffset limite, DateTimeOffset serviciosHasta, CancellationToken ct = default) =>
        solicitudes.ListarAbiertasAntesDeAsync(limite, serviciosHasta, ct);

    public Task<IReadOnlyList<SolicitudTransporte>> ListarPendientesHastaAsync(DateTimeOffset serviciosHasta, CancellationToken ct = default) =>
        solicitudes.ListarPendientesHastaAsync(serviciosHasta, ct);
}
