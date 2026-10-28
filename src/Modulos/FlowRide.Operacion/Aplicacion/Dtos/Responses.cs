using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Operacion.Aplicacion;
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

namespace FlowRide.Operacion.Aplicacion.Dtos;

public sealed record ViajeRealizadoReporte(
    Guid ViajeId, Guid ConductorId, Guid VehiculoId, Guid PasajeroId, Guid? ClienteEmpresarialId, DateTimeOffset ProgramadoPara, DateTimeOffset? IniciadoEn, DateTimeOffset? FinalizadoEn);

public sealed record SolicitudesPorEstadoReporte(EstadoSolicitud Estado, int Cantidad);

public sealed record ConductorAsignadoReporte(Guid ConductorId, string Nombre, int Asignaciones, int Rechazos, int ViajesFinalizados);

public sealed record ClienteOperacionReporte(Guid? ClienteEmpresarialId, string Nombre, int Solicitudes, int Finalizadas, int Canceladas);

public sealed record IndicadoresResponse(
    int Solicitudes,
    int Finalizadas,
    int Canceladas,
    int SinConductor,
    double PorcentajeAsignacionAutomatica,
    double PorcentajePuntualidad,
    double CalificacionPromedio);
