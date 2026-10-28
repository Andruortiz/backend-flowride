using System.Security.Claims;
using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Inicializador.Seguridad;

/// <summary>
/// El actor sale del token (reclamos ya validados) o, en trabajos de fondo, es "sistema".
/// El bot de WhatsApp puede fijarlo explícitamente con <see cref="Establecer"/>.
/// </summary>
internal sealed class ContextoActor(IHttpContextAccessor http) : IContextoActorEstablecible
{
    private (Guid Id, string Nombre, RolUsuario Rol)? _fijado;

    public Guid? UsuarioId => _fijado?.Id ?? (Guid.TryParse(Usuario?.FindFirstValue(Reclamos.UsuarioId), out var id) ? id : null);

    public string Nombre => _fijado?.Nombre ?? Usuario?.FindFirstValue(Reclamos.Nombre) ?? "sistema";

    public RolUsuario? Rol => _fijado?.Rol ?? (Enum.TryParse<RolUsuario>(Usuario?.FindFirstValue(Reclamos.Rol), out var rol) ? rol : null);

    private ClaimsPrincipal? Usuario => http.HttpContext?.User;

    public void Establecer(Guid usuarioId, string nombre, RolUsuario rol) => _fijado = (usuarioId, nombre, rol);
}
