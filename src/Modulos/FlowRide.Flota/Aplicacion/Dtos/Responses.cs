using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Flota.Aplicacion.Dtos;

public sealed record DocumentoResponse(TipoDocumento Tipo, string Numero, DateOnly VenceEl, EstadoVigencia Estado);

public sealed record TurnoResponse(DayOfWeek Dia, TimeOnly Inicio, TimeOnly Fin);

public sealed record ConductorResponse(
    Guid Id,
    Guid UsuarioId,
    string Nombre,
    bool Activo,
    bool Disponible,
    string? MotivoNoDisponible,
    Guid? VehiculoId,
    DocumentoResponse Licencia,
    IReadOnlyList<TurnoResponse> Turnos);

public sealed record VehiculoResponse(
    Guid Id, string Placa, int Capacidad, bool Habilitado, IReadOnlyList<DocumentoResponse> Documentos);
