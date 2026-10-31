using FlowRide.Nucleo.Seguridad;

namespace FlowRide.Nucleo.Seguridad;

public interface IContextoActorEstablecible : IContextoActor
{
    void Establecer(Guid usuarioId, string nombre, RolUsuario rol);
}
