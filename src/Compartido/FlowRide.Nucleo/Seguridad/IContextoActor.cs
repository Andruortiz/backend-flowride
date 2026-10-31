using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Nucleo.Seguridad;

public interface IContextoActor
{
    Guid? UsuarioId { get; }

    string Nombre { get; }

    RolUsuario? Rol { get; }
}
