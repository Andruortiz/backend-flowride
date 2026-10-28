using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;

namespace FlowRide.Identidad.Aplicacion;

internal static class Mapeos
{
    public static UsuarioResponse A(this Usuario u) =>
        new(u.Id, u.Nombre, u.Correo, u.Telefono, u.CodigoInterno, u.Rol, u.Activo, u.CreadoEn);
}
