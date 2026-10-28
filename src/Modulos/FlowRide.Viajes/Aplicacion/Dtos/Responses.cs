using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Viajes.Aplicacion.Dtos;

public sealed record HitoResponse(DateTimeOffset Instante, string Tipo, string Detalle);

public sealed record ViajeResponse(
    Guid Id,
    Guid SolicitudId,
    Guid AsignacionId,
    Guid PasajeroId,
    Guid ConductorId,
    Guid VehiculoId,
    EstadoViaje Estado,
    DateTimeOffset ProgramadoPara,
    DateTimeOffset EtaProgramada,
    DateTimeOffset? IniciadoEn,
    DateTimeOffset? FinalizadoEn,
    bool AlertaEtaActiva,
    IReadOnlyList<HitoResponse> Hitos);

public sealed record CalificacionResponse(Guid Id, Guid ViajeId, int Puntaje, string? Comentario, DateTimeOffset CreadaEn);

public sealed record UbicacionResponse(Guid VehiculoId, double Latitud, double Longitud, DateTimeOffset TimestampOrigen, bool Duplicada);
