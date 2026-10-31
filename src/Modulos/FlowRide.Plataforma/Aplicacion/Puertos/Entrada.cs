using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion;
using FlowRide.Plataforma.Aplicacion.Dtos;

namespace FlowRide.Plataforma.Aplicacion.Puertos;

public interface IServicioParametros
{
    Task<int> ValorAsync(string clave, CancellationToken ct = default);

    Task<IReadOnlyList<ParametroResponse>> ListarAsync(CancellationToken ct = default);

    Task<ParametroResponse> ActualizarAsync(string clave, ActualizarParametroRequest request, CancellationToken ct = default);
}

public interface IServicioAuditoria
{
    Task RegistrarAsync(string accion, string recurso, string? motivo = null, string? antes = null, string? despues = null, CancellationToken ct = default);

    Task RegistrarRechazoAsync(string accion, string recurso, string motivo, CancellationToken ct = default);

    Task<IReadOnlyList<AuditoriaResponse>> ConsultarAsync(string? accion, string? recurso, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default);
}

public interface IServicioOperador
{
    Task<OperadorResponse> ObtenerAsync(CancellationToken ct = default);

    Task<OperadorResponse> ActualizarAsync(ActualizarOperadorRequest request, CancellationToken ct = default);

    Task<TimeZoneInfo> ZonaAsync(CancellationToken ct = default);
}
