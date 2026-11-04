using FlowRide.Flota.Aplicacion;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Seguridad;
using NSubstitute;

namespace FlowRide.Pruebas.Servicios;

public class ServicioFlotaTests
{
    private readonly DoblesComunes _comunes = new();
    private readonly IConductorRepository _conductores = Substitute.For<IConductorRepository>();
    private readonly IVehiculoRepository _vehiculos = Substitute.For<IVehiculoRepository>();
    private readonly IConsultaIdentidad _identidad = Substitute.For<IConsultaIdentidad>();
    private readonly ServicioFlota _servicio;

    public ServicioFlotaTests()
    {
        _servicio = new ServicioFlota(
            _conductores, _vehiculos, _identidad, _comunes.Parametros, _comunes.Operador, _comunes.Auditoria, _comunes.Unidad, _comunes.Reloj);
    }

    private static Usuario UsuarioConductor(int n = 1) =>
        Usuario.Crear($"Conductor {n}", null, $"+57300{n:D7}", $"C-{n:D3}", RolUsuario.Conductor, DoblesComunes.Ahora);

    private static DocumentoRequest Licencia(int dias = 365) =>
        new(TipoDocumento.LicenciaConduccion, "L-1", DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(dias)));

    private Conductor ConductorExistente(out Usuario usuario)
    {
        usuario = UsuarioConductor();
        var conductor = Conductor.Crear(usuario, DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, "L-1", DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(365))));
        _conductores.ObtenerAsync(conductor.Id, Arg.Any<CancellationToken>()).Returns(conductor);
        _identidad.ObtenerUsuarioAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        return conductor;
    }

    private Vehiculo VehiculoExistente(string placa = "ABC123")
    {
        var vehiculo = Vehiculo.Crear(placa, 4);
        _vehiculos.ObtenerAsync(vehiculo.Id, Arg.Any<CancellationToken>()).Returns(vehiculo);

        return vehiculo;
    }

    [Fact]
    public async Task CrearConductorRegistraAlConductorYLoAudita()
    {
        var usuario = UsuarioConductor();
        _identidad.ObtenerUsuarioAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var respuesta = await _servicio.CrearConductorAsync(new CrearConductorRequest(usuario.Id, Licencia()));

        Assert.Equal(usuario.Nombre, respuesta.Nombre);
        Assert.True(respuesta.Activo);
        await _conductores.Received(1).AgregarAsync(Arg.Any<Conductor>(), Arg.Any<CancellationToken>());
        await _comunes.VerificarGuardadoAsync("conductor.crear");
    }

    [Fact]
    public async Task CrearConductorConUsuarioInexistenteFalla()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            _servicio.CrearConductorAsync(new CrearConductorRequest(Guid.NewGuid(), Licencia())));

        Assert.Equal("error.el_usuario_no_existe", error.Clave);
    }

    [Fact]
    public async Task CrearConductorParaUnUsuarioQueYaEsConductorFalla()
    {
        var conductor = ConductorExistente(out var usuario);
        _conductores.ObtenerPorUsuarioAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(conductor);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            _servicio.CrearConductorAsync(new CrearConductorRequest(usuario.Id, Licencia())));

        Assert.Equal("error.el_usuario_ya_es_conductor", error.Clave);
    }

    [Fact]
    public async Task ObtenerConductorDevuelveElNombreDelUsuario()
    {
        var conductor = ConductorExistente(out var usuario);

        var respuesta = await _servicio.ObtenerConductorAsync(conductor.Id);

        Assert.Equal(usuario.Nombre, respuesta.Nombre);
    }

    [Fact]
    public async Task ObtenerConductorInexistenteFalla()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() => _servicio.ObtenerConductorAsync(Guid.NewGuid()));

        Assert.Equal("error.el_conductor_no_existe", error.Clave);
    }

    [Fact]
    public async Task ListarConductoresIncluyeElNombreDeCadaUsuario()
    {
        var conductor = ConductorExistente(out var usuario);
        _conductores.ListarAsync(true, Arg.Any<CancellationToken>()).Returns([conductor]);
        _identidad.ObtenerUsuariosAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Usuario> { [usuario.Id] = usuario });

        var lista = await _servicio.ListarConductoresAsync(true);

        Assert.Equal(usuario.Nombre, Assert.Single(lista).Nombre);
    }

    [Fact]
    public async Task UnConductorSeMarcaNoDisponibleYVuelveADisponible()
    {
        var conductor = ConductorExistente(out _);

        var ausente = await _servicio.MarcarNoDisponibleAsync(conductor.Id, new MarcarNoDisponibleRequest("Cita médica"));
        var presente = await _servicio.MarcarDisponibleAsync(conductor.Id);

        Assert.False(ausente.Disponible);
        Assert.Equal("Cita médica", ausente.MotivoNoDisponible);
        Assert.True(presente.Disponible);
    }

    [Theory]
    [InlineData(false, "conductor.inactivar")]
    [InlineData(true, "conductor.activar")]
    public async Task CambiarEstadoDelConductorAudita(bool activo, string accion)
    {
        var conductor = ConductorExistente(out _);

        var respuesta = await _servicio.CambiarEstadoConductorAsync(conductor.Id, activo);

        Assert.Equal(activo, respuesta.Activo);
        await _comunes.VerificarGuardadoAsync(accion);
    }

    [Fact]
    public async Task SeAsignaYSeQuitaElVehiculoDeUnConductor()
    {
        var conductor = ConductorExistente(out _);
        var vehiculo = VehiculoExistente();

        var asignado = await _servicio.AsignarVehiculoAsync(conductor.Id, new AsignarVehiculoRequest(vehiculo.Id));
        var quitado = await _servicio.QuitarVehiculoAsync(conductor.Id);

        Assert.Equal(vehiculo.Id, asignado.VehiculoId);
        Assert.Null(quitado.VehiculoId);
    }

    [Fact]
    public async Task AsignarUnVehiculoInexistenteFalla()
    {
        var conductor = ConductorExistente(out _);

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            _servicio.AsignarVehiculoAsync(conductor.Id, new AsignarVehiculoRequest(Guid.NewGuid())));

        Assert.Equal("error.el_vehiculo_no_existe", error.Clave);
    }

    [Fact]
    public async Task DefinirTurnosReemplazaLosTurnosDelConductor()
    {
        var conductor = ConductorExistente(out _);
        var turnos = new DefinirTurnosRequest([
            new TurnoRequest(DayOfWeek.Monday, new TimeOnly(6, 0), new TimeOnly(14, 0)),
            new TurnoRequest(DayOfWeek.Tuesday, new TimeOnly(6, 0), new TimeOnly(14, 0)),
        ]);

        var respuesta = await _servicio.DefinirTurnosAsync(conductor.Id, turnos);

        Assert.Equal(2, respuesta.Turnos.Count);
        await _comunes.VerificarGuardadoAsync("conductor.definir_turnos");
    }

    [Fact]
    public async Task RenovarLaLicenciaActualizaElVencimiento()
    {
        var conductor = ConductorExistente(out _);

        var respuesta = await _servicio.RenovarLicenciaAsync(conductor.Id, Licencia(730));

        Assert.NotNull(respuesta.Licencia);
        await _comunes.VerificarGuardadoAsync("conductor.renovar_licencia");
    }

    [Fact]
    public async Task CrearVehiculoLoRegistraConSuPlaca()
    {
        var respuesta = await _servicio.CrearVehiculoAsync(new CrearVehiculoRequest("ABC123", 4));

        Assert.Equal("ABC123", respuesta.Placa);
        Assert.Equal(4, respuesta.Capacidad);
        await _vehiculos.Received(1).AgregarAsync(Arg.Any<Vehiculo>(), Arg.Any<CancellationToken>());
        await _comunes.VerificarGuardadoAsync("vehiculo.crear");
    }

    [Fact]
    public async Task CrearVehiculoConPlacaRepetidaFalla()
    {
        _vehiculos.ObtenerPorPlacaAsync("ABC123", Arg.Any<CancellationToken>()).Returns(Vehiculo.Crear("ABC123", 4));

        var error = await Assert.ThrowsAsync<ConflictException>(() => _servicio.CrearVehiculoAsync(new CrearVehiculoRequest("ABC123", 4)));

        Assert.Equal("error.ya_existe_un_vehiculo_con_esa_placa", error.Clave);
    }

    [Fact]
    public async Task ObtenerYListarVehiculosDevuelvenLosRegistrados()
    {
        var vehiculo = VehiculoExistente();
        _vehiculos.ListarAsync(Arg.Any<CancellationToken>()).Returns([vehiculo]);

        var uno = await _servicio.ObtenerVehiculoAsync(vehiculo.Id);
        var lista = await _servicio.ListarVehiculosAsync();

        Assert.Equal(vehiculo.Placa, uno.Placa);
        Assert.Equal(vehiculo.Placa, Assert.Single(lista).Placa);
    }

    [Fact]
    public async Task ObtenerUnVehiculoInexistenteFalla()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _servicio.ObtenerVehiculoAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RegistrarUnDocumentoLoAgregaAlVehiculo()
    {
        var vehiculo = VehiculoExistente();
        var documento = new DocumentoRequest(TipoDocumento.Soat, "S-1", DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(200)));

        var respuesta = await _servicio.RegistrarDocumentoVehiculoAsync(vehiculo.Id, documento);

        Assert.Contains(respuesta.Documentos, d => d.Numero == "S-1");
        await _comunes.VerificarGuardadoAsync("vehiculo.documento");
    }

    [Theory]
    [InlineData(false, "vehiculo.deshabilitar")]
    [InlineData(true, "vehiculo.habilitar")]
    public async Task CambiarLaHabilitacionDelVehiculoAudita(bool habilitado, string accion)
    {
        var vehiculo = VehiculoExistente();

        var respuesta = await _servicio.CambiarHabilitacionVehiculoAsync(vehiculo.Id, habilitado);

        Assert.Equal(habilitado, respuesta.Habilitado);
        await _comunes.VerificarGuardadoAsync(accion);
    }
}
