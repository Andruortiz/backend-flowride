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

public sealed record ActualizarParametroRequest(int Valor);

public sealed record ActualizarOperadorRequest(string RazonSocial, string Nit, string CorreoContacto, string ZonaHoraria);
