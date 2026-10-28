using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Operacion.Aplicacion.Dtos;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;

namespace FlowRide.Operacion.Aplicacion;

internal sealed class ServicioReportes(IReportesQuery consultas, IServicioParametros parametros) : IServicioReportes
{
    private const int MaximoDias = 366;

    public Task<IReadOnlyList<ViajeRealizadoReporte>> ViajesRealizadosAsync(DateTimeOffset desde, DateTimeOffset hasta, Guid? clienteEmpresarialId, CancellationToken ct = default) =>
        consultas.ViajesRealizadosAsync(desde, ValidarRango(desde, hasta), clienteEmpresarialId, ct);

    public Task<IReadOnlyList<SolicitudesPorEstadoReporte>> SolicitudesPorEstadoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        consultas.SolicitudesPorEstadoAsync(desde, ValidarRango(desde, hasta), ct);

    public Task<IReadOnlyList<ConductorAsignadoReporte>> ConductoresAsignadosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        consultas.ConductoresAsignadosAsync(desde, ValidarRango(desde, hasta), ct);

    public Task<IReadOnlyList<ClienteOperacionReporte>> OperacionPorClienteAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default) =>
        consultas.OperacionPorClienteAsync(desde, ValidarRango(desde, hasta), ct);

    public async Task<IndicadoresResponse> IndicadoresAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default)
    {
        var umbral = await parametros.ValorAsync(FlowRide.Plataforma.Dominio.CatalogoParametros.UmbralEtaMinutos, ct);

        return await consultas.IndicadoresAsync(desde, ValidarRango(desde, hasta), umbral, ct);
    }

    private static DateTimeOffset ValidarRango(DateTimeOffset desde, DateTimeOffset hasta)
    {
        if (hasta <= desde)
        {
            throw new DomainException("El rango del reporte es inválido: 'hasta' debe ser posterior a 'desde'.");
        }

        if ((hasta - desde).TotalDays > MaximoDias)
        {
            throw new DomainException($"El rango del reporte no puede superar {MaximoDias} días.");
        }

        return hasta;
    }
}
