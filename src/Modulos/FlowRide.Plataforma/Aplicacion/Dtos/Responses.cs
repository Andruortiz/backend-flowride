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
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Plataforma.Aplicacion.Dtos;

public sealed record ParametroResponse(string Clave, string Descripcion, string Unidad, int Minimo, int Maximo, int Defecto, int Valor, int Version);

public sealed record AuditoriaResponse(
    Guid Id, Guid? ActorId, string ActorNombre, string Accion, string Recurso, ResultadoAuditoria Resultado, string? Motivo, DateTimeOffset Instante);

public sealed record OperadorResponse(Guid Id, string RazonSocial, string Nit, string CorreoContacto, string ZonaHoraria, bool Activo);

public sealed record PaginaResponse<T>(IReadOnlyList<T> Items, int Pagina, int Tamano);
