using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Nucleo.Seguridad;

/// <summary>
/// Permite que un canal sin token (el bot de WhatsApp) actúe como el usuario identificado por su teléfono.
/// Solo vive durante la solicitud o el trabajo en curso.
/// </summary>
public interface IContextoActorEstablecible : IContextoActor
{
    void Establecer(Guid usuarioId, string nombre, RolUsuario rol);
}
