using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Operacion.Aplicacion;
using FlowRide.Operacion.Aplicacion.Dtos;
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

namespace FlowRide.Operacion.Aplicacion.Puertos;

public sealed record ResultadoPlanificacion(int Recordatorios, int AlertasSinConductor, int AlertasDocumentos, int Asignadas);

public interface IServicioPlanificacion
{
    Task<ResultadoPlanificacion> EjecutarAsync(CancellationToken ct = default);
}

public interface IServicioReportes
{
    Task<IReadOnlyList<ViajeRealizadoReporte>> ViajesRealizadosAsync(DateTimeOffset desde, DateTimeOffset hasta, Guid? clienteEmpresarialId, CancellationToken ct = default);

    Task<IReadOnlyList<SolicitudesPorEstadoReporte>> SolicitudesPorEstadoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);

    Task<IReadOnlyList<ConductorAsignadoReporte>> ConductoresAsignadosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteOperacionReporte>> OperacionPorClienteAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);

    Task<IndicadoresResponse> IndicadoresAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct = default);
}
