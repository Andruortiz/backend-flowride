using System.Text.Json.Serialization;
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

public sealed record CrearSolicitudRequest(
    Guid? PasajeroId,
    string Origen,
    string Destino,
    [property: JsonRequired] DateTimeOffset FechaHoraProgramada,
    [property: JsonRequired] bool Urgente,
    [property: JsonRequired] bool EsPersonal,
    Guid? ItinerarioId,
    double? DestinoLatitud,
    double? DestinoLongitud);

public sealed record ModificarSolicitudRequest(string Origen, string Destino, [property: JsonRequired] DateTimeOffset FechaHoraProgramada);

public sealed record CancelarSolicitudRequest(string Motivo);
