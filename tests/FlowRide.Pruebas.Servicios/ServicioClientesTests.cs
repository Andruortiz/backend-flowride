using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Clientes.Dominio;
using FlowRide.Nucleo.Excepciones;
using NSubstitute;

namespace FlowRide.Pruebas.Servicios;

public class ServicioClientesTests
{
    private readonly DoblesComunes _comunes = new();
    private readonly IClienteEmpresarialRepository _clientes = Substitute.For<IClienteEmpresarialRepository>();
    private readonly IItinerarioRepository _itinerarios = Substitute.For<IItinerarioRepository>();
    private readonly ServicioClientes _servicio;

    public ServicioClientesTests()
    {
        _servicio = new ServicioClientes(_clientes, _itinerarios, _comunes.Auditoria, _comunes.Unidad, _comunes.Reloj);
    }

    private ClienteEmpresarial ClienteExistente()
    {
        var cliente = ClienteEmpresarial.Crear("Acme", "900123");
        _clientes.ObtenerAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        return cliente;
    }

    [Fact]
    public async Task CrearUnClienteLoRegistraYLoAudita()
    {
        var respuesta = await _servicio.CrearAsync(new CrearClienteEmpresarialRequest("Acme", "900123"));

        Assert.Equal("Acme", respuesta.Nombre);
        Assert.True(respuesta.Activo);
        await _clientes.Received(1).AgregarAsync(Arg.Any<ClienteEmpresarial>(), Arg.Any<CancellationToken>());
        await _comunes.VerificarGuardadoAsync("cliente.crear");
    }

    [Fact]
    public async Task ObtenerYListarDevuelvenLosClientes()
    {
        var cliente = ClienteExistente();
        _clientes.ListarAsync(Arg.Any<CancellationToken>()).Returns([cliente]);

        var uno = await _servicio.ObtenerAsync(cliente.Id);
        var lista = await _servicio.ListarAsync();

        Assert.Equal("Acme", uno.Nombre);
        Assert.Single(lista);
    }

    [Fact]
    public async Task ObtenerUnClienteInexistenteFalla()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() => _servicio.ObtenerAsync(Guid.NewGuid()));

        Assert.Equal("error.el_cliente_empresarial_no_existe", error.Clave);
    }

    [Fact]
    public async Task ActualizarCambiaElNombreDelCliente()
    {
        var cliente = ClienteExistente();

        var respuesta = await _servicio.ActualizarAsync(cliente.Id, new CrearClienteEmpresarialRequest("Acme Colombia", "900123"));

        Assert.Equal("Acme Colombia", respuesta.Nombre);
        await _comunes.VerificarGuardadoAsync("cliente.actualizar");
    }

    [Theory]
    [InlineData(false, "cliente.inactivar")]
    [InlineData(true, "cliente.activar")]
    public async Task CambiarElEstadoDelClienteAudita(bool activo, string accion)
    {
        var cliente = ClienteExistente();

        var respuesta = await _servicio.CambiarEstadoAsync(cliente.Id, activo);

        Assert.Equal(activo, respuesta.Activo);
        await _comunes.VerificarGuardadoAsync(accion);
    }

    [Fact]
    public async Task CrearUnItinerarioLoGuardaParaElCliente()
    {
        var cliente = ClienteExistente();
        var fecha = DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(10));

        var respuesta = await _servicio.CrearItinerarioAsync(
            new CrearItinerarioRequest("AV100", cliente.Id, "Calle 1 # 2-3", "Aeropuerto", fecha, new TimeOnly(5, 0), new TimeOnly(6, 0)));

        Assert.Equal("AV100", respuesta.CodigoServicio);
        await _itinerarios.Received(1).AgregarAsync(Arg.Any<Itinerario>(), Arg.Any<CancellationToken>());
        await _comunes.VerificarGuardadoAsync("itinerario.crear");
    }

    [Fact]
    public async Task CrearUnItinerarioParaUnClienteInexistenteFalla()
    {
        var fecha = DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(10));

        await Assert.ThrowsAsync<NotFoundException>(() => _servicio.CrearItinerarioAsync(
            new CrearItinerarioRequest("AV100", Guid.NewGuid(), "Calle 1 # 2-3", "Aeropuerto", fecha, new TimeOnly(5, 0), new TimeOnly(6, 0))));
    }

    [Fact]
    public async Task ListarItinerariosDevuelveLosDelCliente()
    {
        var cliente = ClienteExistente();
        var fecha = DateOnly.FromDateTime(DoblesComunes.Ahora.UtcDateTime.AddDays(10));
        var itinerario = Itinerario.Crear("AV100", cliente.Id, "Calle 1 # 2-3", "Aeropuerto", fecha, new TimeOnly(5, 0), new TimeOnly(6, 0), DoblesComunes.Ahora);
        _itinerarios.ListarAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns([itinerario]);

        var lista = await _servicio.ListarItinerariosAsync(cliente.Id);

        Assert.Equal("AV100", Assert.Single(lista).CodigoServicio);
    }
}
