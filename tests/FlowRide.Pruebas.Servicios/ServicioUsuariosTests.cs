using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;
using NSubstitute;

namespace FlowRide.Pruebas.Servicios;

public class ServicioUsuariosTests
{
    private readonly DoblesComunes _comunes = new();
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IPasajeroRepository _pasajeros = Substitute.For<IPasajeroRepository>();
    private readonly IConsultaClientes _clientes = Substitute.For<IConsultaClientes>();
    private readonly ServicioUsuarios _servicio;

    public ServicioUsuariosTests()
    {
        _servicio = new ServicioUsuarios(_usuarios, _pasajeros, _clientes, _comunes.Auditoria, _comunes.Unidad, _comunes.Reloj);
    }

    private Usuario UsuarioExistente(RolUsuario rol = RolUsuario.AdministradorOperador)
    {
        var usuario = rol is RolUsuario.Pasajero or RolUsuario.Conductor
            ? Usuario.Crear("Carlos Ruiz", null, "+573001112233", "U-001", rol, DoblesComunes.Ahora)
            : Usuario.Crear("Ana Gómez", "ana@flowride.co", null, "U-001", rol, DoblesComunes.Ahora);
        _usuarios.ObtenerAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        return usuario;
    }

    [Fact]
    public async Task CrearUnAdministradorLoRegistraYLoAudita()
    {
        var respuesta = await _servicio.CrearAsync(new CrearUsuarioRequest("Ana Gómez", "ana@flowride.co", null, "ADM-1", RolUsuario.AdministradorOperador, null));

        Assert.Equal(RolUsuario.AdministradorOperador, respuesta.Rol);
        await _usuarios.Received(1).AgregarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
        await _comunes.VerificarGuardadoAsync("usuario.crear");
    }

    [Fact]
    public async Task CrearUnPasajeroLoVinculaConSuClienteEmpresarial()
    {
        var cliente = ClienteEmpresarial.Crear("Acme", "900123");
        _clientes.ObtenerClienteAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var respuesta = await _servicio.CrearAsync(new CrearUsuarioRequest("Carlos Ruiz", null, "+573001112233", "PAS-1", RolUsuario.Pasajero, cliente.Id));

        Assert.Equal(RolUsuario.Pasajero, respuesta.Rol);
        await _pasajeros.Received(1).AgregarAsync(Arg.Any<Pasajero>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnPasajeroSinClienteEmpresarialSeRechaza()
    {
        var error = await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Carlos Ruiz", null, "+573001112233", "PAS-1", RolUsuario.Pasajero, null)));

        Assert.Equal("error.el_pasajero_debe_pertenecer_a_un_cliente", error.Clave);
    }

    [Fact]
    public async Task UnPasajeroConClienteInexistenteSeRechaza()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Carlos Ruiz", null, "+573001112233", "PAS-1", RolUsuario.Pasajero, Guid.NewGuid())));

        Assert.Equal("error.el_cliente_empresarial_no_existe", error.Clave);
    }

    [Fact]
    public async Task UnPasajeroConClienteInactivoSeRechaza()
    {
        var cliente = ClienteEmpresarial.Crear("Acme", "900123");
        cliente.Inactivar();
        _clientes.ObtenerClienteAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Carlos Ruiz", null, "+573001112233", "PAS-1", RolUsuario.Pasajero, cliente.Id)));

        Assert.Equal("error.el_cliente_empresarial_esta_inactivo", error.Clave);
    }

    [Fact]
    public async Task UnCodigoInternoRepetidoSeRechaza()
    {
        _usuarios.ExisteCodigoAsync("ADM-1", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Ana Gómez", "ana@flowride.co", null, "ADM-1", RolUsuario.AdministradorOperador, null)));

        Assert.Equal("error.ya_existe_un_usuario_con_ese_codigo", error.Clave);
    }

    [Fact]
    public async Task UnCorreoRepetidoSeRechaza()
    {
        _usuarios.ExisteCorreoAsync("ana@flowride.co", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Ana Gómez", "ana@flowride.co", null, "ADM-1", RolUsuario.AdministradorOperador, null)));

        Assert.Equal("error.ya_existe_un_usuario_con_ese_correo", error.Clave);
    }

    [Fact]
    public async Task UnTelefonoRepetidoSeRechaza()
    {
        _usuarios.ExisteTelefonoAsync("+573001112233", null, Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearAsync(new CrearUsuarioRequest("Luis Mora", null, "+573001112233", "CON-1", RolUsuario.Conductor, null)));

        Assert.Equal("error.ya_existe_un_usuario_con_ese_telefono", error.Clave);
    }

    [Fact]
    public async Task ObtenerYListarDevuelvenLosUsuarios()
    {
        var usuario = UsuarioExistente();
        _usuarios.ListarAsync(null, null, 1, 20, Arg.Any<CancellationToken>()).Returns([usuario]);

        var uno = await _servicio.ObtenerAsync(usuario.Id);
        var lista = await _servicio.ListarAsync(null, null, 1, 20);

        Assert.Equal(usuario.Nombre, uno.Nombre);
        Assert.Single(lista);
    }

    [Fact]
    public async Task ObtenerUnUsuarioInexistenteFalla()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() => _servicio.ObtenerAsync(Guid.NewGuid()));

        Assert.Equal("error.el_usuario_no_existe", error.Clave);
    }

    [Fact]
    public async Task ActualizarCambiaLosDatosDelUsuario()
    {
        var usuario = UsuarioExistente(RolUsuario.Conductor);

        var respuesta = await _servicio.ActualizarAsync(usuario.Id, new ActualizarUsuarioRequest("Carlos R. Ruiz", null, "+573009998877"));

        Assert.Equal("Carlos R. Ruiz", respuesta.Nombre);
        Assert.Equal("+573009998877", respuesta.Telefono);
        await _comunes.VerificarGuardadoAsync("usuario.actualizar");
    }

    [Fact]
    public async Task UnAdministradorPuedeCambiarDeRol()
    {
        var usuario = UsuarioExistente();

        var respuesta = await _servicio.CambiarRolAsync(usuario.Id, new CambiarRolRequest(RolUsuario.OperadorLogistico));

        Assert.Equal(RolUsuario.OperadorLogistico, respuesta.Rol);
        await _comunes.VerificarGuardadoAsync("usuario.cambiar_rol");
    }

    [Fact]
    public async Task UnConductorNoPuedeCambiarDeRol()
    {
        var usuario = UsuarioExistente(RolUsuario.Conductor);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.CambiarRolAsync(usuario.Id, new CambiarRolRequest(RolUsuario.AdministradorOperador)));

        Assert.Equal("error.el_rol_de_un_pasajero_o_conductor", error.Clave);
    }

    [Fact]
    public async Task DesactivarYActivarUnUsuarioCambianSuEstado()
    {
        var usuario = UsuarioExistente();

        await _servicio.DesactivarAsync(usuario.Id);
        Assert.False(usuario.Activo);

        await _servicio.ActivarAsync(usuario.Id);
        Assert.True(usuario.Activo);
    }
}
