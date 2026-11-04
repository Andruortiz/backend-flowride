using System.Text.Json.Serialization;
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

public sealed record DocumentoRequest([property: JsonRequired] TipoDocumento Tipo, string Numero, [property: JsonRequired] DateOnly VenceEl);

public sealed record TurnoRequest(DayOfWeek Dia, TimeOnly Inicio, TimeOnly Fin);

public sealed record CrearConductorRequest([property: JsonRequired] Guid UsuarioId, DocumentoRequest Licencia);

public sealed record DefinirTurnosRequest(IReadOnlyList<TurnoRequest> Turnos);

public sealed record MarcarNoDisponibleRequest(string Motivo);

public sealed record CrearVehiculoRequest(string Placa, [property: JsonRequired] int Capacidad);

public sealed record AsignarVehiculoRequest([property: JsonRequired] Guid VehiculoId);
