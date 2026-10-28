using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Nucleo.Seguridad;

/// <summary>Quién ejecuta la operación. Lo resuelve la API desde el token; los casos de uso nunca leen HTTP.</summary>
public interface IContextoActor
{
    Guid? UsuarioId { get; }

    string Nombre { get; }

    RolUsuario? Rol { get; }
}
