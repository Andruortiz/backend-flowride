using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Clientes.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Identidad.Dominio;

public sealed class Pasajero : Entidad
{
    public const int MaximoDomicilio = 300;

    private Pasajero(Guid id, Guid usuarioId, Guid clienteEmpresarialId)
        : base(id)
    {
        UsuarioId = usuarioId;
        ClienteEmpresarialId = clienteEmpresarialId;
    }

    public Guid UsuarioId { get; }

    public Guid ClienteEmpresarialId { get; private set; }

    public string? Domicilio { get; private set; }

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

    public void DefinirDomicilio(string domicilio)
    {
        if (TextHelper.IsEmptyWithTrim(domicilio))
        {
            throw new DomainException("El domicilio es obligatorio.");
        }

        if (domicilio.Trim().Length > MaximoDomicilio)
        {
            throw new DomainException($"El domicilio no puede superar {MaximoDomicilio} caracteres.");
        }

        Domicilio = domicilio.Trim();
    }

    private static void ValidarCliente(Guid clienteEmpresarialId)
    {
        if (UuidHelper.IsDefault(clienteEmpresarialId))
        {
            throw new DomainException("El pasajero debe pertenecer a un cliente empresarial.");
        }
    }
}
