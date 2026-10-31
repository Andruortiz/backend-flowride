using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Clientes.Aplicacion.Dtos;

public sealed record CrearClienteEmpresarialRequest(string Nombre, string? Nit);

public sealed record CrearItinerarioRequest(
    string CodigoServicio, Guid? ClienteEmpresarialId, string Origen, string Destino, DateOnly Fecha, TimeOnly HoraSalida, TimeOnly HoraLlegada, Guid? PasajeroId = null);
