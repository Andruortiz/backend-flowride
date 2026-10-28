using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;

namespace FlowRide.Plataforma.Aplicacion;

internal static class Mapeos
{
    public static AuditoriaResponse A(this RegistroAuditoria r) =>
        new(r.Id, r.ActorId, r.ActorNombre, r.Accion, r.Recurso, r.Resultado, r.Motivo, r.Instante);

    public static OperadorResponse A(this Operador o) => new(o.Id, o.RazonSocial, o.Nit, o.CorreoContacto, o.ZonaHoraria, o.Activo);
}
