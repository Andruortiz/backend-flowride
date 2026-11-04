using FlowRide.Nucleo.Excepciones;

namespace FlowRide.Mensajes;

public static class CatalogoExtensiones
{
    public static string Texto(this ICatalogoMensajes catalogo, MensajeClave mensaje) =>
        catalogo.Texto(
            mensaje.Clave,
            mensaje.Variables.ToDictionary(v => v.Key, v => catalogo.Contiene(v.Value) ? catalogo.Texto(v.Value) : v.Value));

    public static string Texto(this ICatalogoMensajes catalogo, NegocioException excepcion) =>
        excepcion.Detalles.Count > 0
            ? string.Join(" ", excepcion.Detalles.Select(catalogo.Texto))
            : catalogo.Texto(new MensajeClave(excepcion.Clave, excepcion.Variables));
}
