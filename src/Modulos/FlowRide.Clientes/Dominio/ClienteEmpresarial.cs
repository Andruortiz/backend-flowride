using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Clientes.Dominio;

/// <summary>Empresa atendida por el operador (aerolínea, minera, hospital). No aísla tenants: eso lo hace el Silo.</summary>
public sealed class ClienteEmpresarial : Entidad
{
    private ClienteEmpresarial(Guid id, string nombre, string? nit)
        : base(id)
    {
        Nombre = nombre;
        Nit = nit;
        Activo = true;
    }

    public string Nombre { get; private set; }

    public string? Nit { get; private set; }

    public bool Activo { get; private set; }

    public static ClienteEmpresarial Crear(string nombre, string? nit, Guid? id = null) =>
        new(UuidHelper.GetOrGenerate(id), NombreValido(nombre), NitNormalizado(nit));

    public void Actualizar(string nombre, string? nit)
    {
        Nombre = NombreValido(nombre);
        Nit = NitNormalizado(nit);
    }

    public void Inactivar() => Activo = false;

    public void Activar() => Activo = true;

    private static string NombreValido(string nombre) =>
        TextHelper.IsEmptyWithTrim(nombre)
            ? throw new DomainException("El nombre del cliente empresarial es obligatorio.")
            : nombre.Trim();

    private static string? NitNormalizado(string? nit) =>
        TextHelper.IsEmptyWithTrim(nit) ? null : nit.Trim();
}
