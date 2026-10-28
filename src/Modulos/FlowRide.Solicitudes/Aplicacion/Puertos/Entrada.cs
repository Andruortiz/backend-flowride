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

public interface IServicioSolicitudes
{
    Task<SolicitudResponse> RegistrarAsync(CrearSolicitudRequest request, CancellationToken ct = default);

    Task<SolicitudResponse> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SolicitudResponse>> ListarAsync(
        EstadoSolicitud? estado, Guid? pasajeroId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default);

    Task<SolicitudResponse> ModificarAsync(Guid id, ModificarSolicitudRequest request, CancellationToken ct = default);

    Task<SolicitudResponse> CancelarAsync(Guid id, CancelarSolicitudRequest request, CancellationToken ct = default);
}
