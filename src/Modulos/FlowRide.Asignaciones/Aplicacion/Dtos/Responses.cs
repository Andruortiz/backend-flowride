using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Aplicacion.Dtos;

public sealed record AsignacionResponse(
    Guid Id,
    Guid SolicitudId,
    Guid ConductorId,
    Guid VehiculoId,
    EstadoAsignacion Estado,
    bool EsManual,
    string? Motivo,
    int VersionConfiguracion,
    DecisionAsignacion? Explicacion,
    DateTimeOffset CreadaEn,
    DateTimeOffset? RespondidaEn);

public sealed record ResultadoAsignacionResponse(
    bool Asignada, AsignacionResponse? Asignacion, DecisionAsignacion Decision, EstadoSolicitud EstadoSolicitud);

public sealed record SimulacionAsignacionResponse(Guid? ConductorElegido, DecisionAsignacion Decision);

public sealed record ReglaConfiguradaResponse(string ReglaId, string Nombre, TipoRegla Tipo, bool Obligatoria, bool Activa, int Orden, double Peso);

public sealed record ConfiguracionAsignacionResponse(int Version, DateTimeOffset CreadaEn, IReadOnlyList<ReglaConfiguradaResponse> Reglas);
