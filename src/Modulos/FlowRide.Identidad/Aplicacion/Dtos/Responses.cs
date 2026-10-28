using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Identidad.Aplicacion.Dtos;

public sealed record UsuarioResponse(
    Guid Id, string Nombre, string? Correo, string? Telefono, string CodigoInterno, RolUsuario Rol, bool Activo, DateTimeOffset CreadoEn);

public sealed record PasajeroResponse(Guid Id, Guid UsuarioId, Guid ClienteEmpresarialId, string Nombre, string? Telefono);
