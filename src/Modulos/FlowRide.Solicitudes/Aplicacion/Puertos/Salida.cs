using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Solicitudes.Aplicacion.Puertos;

internal interface ISolicitudRepository
{
    Task<SolicitudTransporte?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<SolicitudTransporte?> ObtenerUltimaDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);

    Task<IReadOnlyList<SolicitudTransporte>> ListarAsync(
        EstadoSolicitud? estado,
        Guid? pasajeroId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int pagina,
        int tamano,
        CancellationToken ct = default);

    /// <summary>Asignadas cuya hora de servicio cae en el rango y aún sin recordatorio.</summary>
    Task<IReadOnlyList<SolicitudTransporte>> ListarParaRecordatorioAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);

    /// <summary>Abiertas (pendientes o en revisión) creadas antes del límite.</summary>
    Task<IReadOnlyList<SolicitudTransporte>> ListarAbiertasAntesDeAsync(DateTimeOffset limite, CancellationToken ct = default);

    Task AgregarAsync(SolicitudTransporte solicitud, CancellationToken ct = default);
}
