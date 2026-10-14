using FlowRide.Dominio.Compartido;
using FlowRide.Transversal.Excepciones;
using FlowRide.Transversal.Helpers;

namespace FlowRide.Dominio.Usuarios;

public sealed class Pasajero : Entidad
{
    private Pasajero(Guid id, Guid usuarioId, Guid clienteEmpresarialId)
        : base(id)
    {
        UsuarioId = usuarioId;
        ClienteEmpresarialId = clienteEmpresarialId;
    }

    public Guid UsuarioId { get; }

    public Guid ClienteEmpresarialId { get; private set; }

    public static Pasajero Crear(Usuario usuario, Guid clienteEmpresarialId, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (usuario.Rol != RolUsuario.Pasajero)
        {
            throw new DomainException("Solo un usuario con rol Pasajero puede tener perfil de pasajero.");
        }

        ValidarCliente(clienteEmpresarialId);

        return new Pasajero(UuidHelper.GetOrGenerate(id), usuario.Id, clienteEmpresarialId);
    }

    public void CambiarClienteEmpresarial(Guid nuevoClienteEmpresarialId)
    {
        ValidarCliente(nuevoClienteEmpresarialId);

        ClienteEmpresarialId = nuevoClienteEmpresarialId;
    }

    private static void ValidarCliente(Guid clienteEmpresarialId)
    {
        if (UuidHelper.IsDefault(clienteEmpresarialId))
        {
            throw new DomainException("El pasajero debe pertenecer a un cliente empresarial.");
        }
    }
}