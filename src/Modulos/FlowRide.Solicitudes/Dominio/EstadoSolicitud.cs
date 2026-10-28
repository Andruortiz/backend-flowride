using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Solicitudes.Dominio;

public enum EstadoSolicitud
{
    Pendiente = 1,
    EnRevision = 2,
    Asignada = 3,
    EnCurso = 4,
    Finalizada = 5,
    Cancelada = 6,
}
