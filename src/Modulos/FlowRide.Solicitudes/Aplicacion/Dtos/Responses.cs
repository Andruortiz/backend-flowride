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
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Solicitudes.Aplicacion.Dtos;

/// <summary>Origen y destino llegan nulos cuando la solicitud es personal y quien consulta es administración u operación (PAS-SEG-01).</summary>
public sealed record SolicitudResponse(
    Guid Id,
    Guid PasajeroId,
    string? Origen,
    string? Destino,
    DateTimeOffset FechaHoraProgramada,
    bool Urgente,
    bool EsPersonal,
    EstadoSolicitud Estado,
    Guid? AsignacionId,
    int Intentos,
    string? MotivoCancelacion,
    DateTimeOffset CreadaEn);
