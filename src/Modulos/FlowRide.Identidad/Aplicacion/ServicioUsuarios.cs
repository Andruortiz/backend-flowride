using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Identidad.Aplicacion;

internal sealed class ServicioUsuarios(
    IUsuarioRepository usuarios,
    IPasajeroRepository pasajeros,
    IConsultaClientes clientes,
    IServicioAuditoria auditoria,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioUsuarios
{
    public async Task<UsuarioResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = Usuario.Crear(request.Nombre, request.Correo, request.Telefono, request.CodigoInterno, request.Rol, reloj.GetUtcNow());

        await ValidarUnicidadAsync(usuario, null, ct);

        if (request.Rol == RolUsuario.Pasajero)
        {
            var clienteId = request.ClienteEmpresarialId ?? throw new DomainException("El pasajero debe pertenecer a un cliente empresarial.");
            var cliente = await clientes.ObtenerClienteAsync(clienteId, ct) ?? throw new NotFoundException("El cliente empresarial no existe.");

            if (!cliente.Activo)
            {
                throw new DomainException("El cliente empresarial está inactivo.");
            }

            await pasajeros.AgregarAsync(Pasajero.Crear(usuario, clienteId), ct);
        }

        await usuarios.AgregarAsync(usuario, ct);
        await auditoria.RegistrarAsync("usuario.crear", $"usuario/{usuario.Id}", despues: $"{usuario.Rol}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return usuario.A();
    }

    public async Task<UsuarioResponse> ObtenerAsync(Guid id, CancellationToken ct = default) => (await Buscar(id, ct)).A();

    public async Task<IReadOnlyList<UsuarioResponse>> ListarAsync(RolUsuario? rol, bool? activo, int pagina, int tamano, CancellationToken ct = default) =>
        [.. (await usuarios.ListarAsync(rol, activo, pagina, tamano, ct)).Select(u => u.A())];

    public async Task<UsuarioResponse> ActualizarAsync(Guid id, ActualizarUsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = await Buscar(id, ct);
        usuario.ActualizarDatos(request.Nombre, request.Correo, request.Telefono);
        await ValidarUnicidadAsync(usuario, usuario.Id, ct);
        await auditoria.RegistrarAsync("usuario.actualizar", $"usuario/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return usuario.A();
    }

    public async Task<UsuarioResponse> CambiarRolAsync(Guid id, CambiarRolRequest request, CancellationToken ct = default)
    {
        var usuario = await Buscar(id, ct);
        var anterior = usuario.Rol;

        if (anterior is RolUsuario.Pasajero or RolUsuario.Conductor && request.Rol != anterior)
        {
            throw new DomainException("El rol de un pasajero o conductor no se cambia; crea un usuario nuevo.");
        }

        usuario.CambiarRol(request.Rol);
        await auditoria.RegistrarAsync("usuario.cambiar_rol", $"usuario/{id}", antes: anterior.ToString(), despues: request.Rol.ToString(), ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return usuario.A();
    }

    public async Task DesactivarAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await Buscar(id, ct);
        usuario.Desactivar(reloj.GetUtcNow());

        await auditoria.RegistrarAsync("usuario.desactivar", $"usuario/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);
    }

    public async Task ActivarAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await Buscar(id, ct);
        usuario.Activar();
        await auditoria.RegistrarAsync("usuario.activar", $"usuario/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);
    }

    private async Task<Usuario> Buscar(Guid id, CancellationToken ct) =>
        await usuarios.ObtenerAsync(id, ct) ?? throw new NotFoundException("El usuario no existe.");

    private async Task ValidarUnicidadAsync(Usuario usuario, Guid? excluir, CancellationToken ct)
    {
        if (excluir is null && await usuarios.ExisteCodigoAsync(usuario.CodigoInterno, null, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese código interno.");
        }

        if (usuario.Correo is not null && await usuarios.ExisteCorreoAsync(usuario.Correo, excluir, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese correo.");
        }

        if (usuario.Telefono is not null && await usuarios.ExisteTelefonoAsync(usuario.Telefono, excluir, ct))
        {
            throw new ConflictException("Ya existe un usuario con ese teléfono.");
        }
    }
}
