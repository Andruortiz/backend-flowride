using FlowRide.Identidad.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Nucleo.Seguridad;

using FlowRide.Contratos;
using FlowRide.Nucleo.Excepciones;


namespace FlowRide.Pruebas.Dominio.Usuarios;

public class PasajeroTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Cliente = Guid.CreateVersion7();

    private static Usuario UsuarioPasajero() =>
        Usuario.Crear("Ana Pérez", null, "+573001234567", "P-001", RolUsuario.Pasajero, Ahora);

    [Fact]
    public void CrearConUsuarioPasajeroYClienteValidoReferenciaAlUsuarioPorId()
    {
        var usuario = UsuarioPasajero();

        var pasajero = Pasajero.Crear(usuario, Cliente);

        Assert.Equal(usuario.Id, pasajero.UsuarioId);
        Assert.Equal(Cliente, pasajero.ClienteEmpresarialId);
        Assert.NotEqual(Guid.Empty, pasajero.Id);
    }

    [Fact]
    public void CrearConUsuarioQueNoEsPasajeroFalla()
    {
        var conductor = Usuario.Crear("Pedro", null, "+573001234567", "C-001", RolUsuario.Conductor, Ahora);

        Assert.Throws<DomainException>(() => Pasajero.Crear(conductor, Cliente));
    }

    [Fact]
    public void CrearSinClienteEmpresarialFalla()
    {
        Assert.Throws<DomainException>(() => Pasajero.Crear(UsuarioPasajero(), Guid.Empty));
    }

    [Fact]
    public void CrearConUsuarioNuloLanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Pasajero.Crear(null!, Cliente));
    }

    [Fact]
    public void CrearConIdLoRespeta()
    {
        var id = Guid.CreateVersion7();

        var pasajero = Pasajero.Crear(UsuarioPasajero(), Cliente, id);

        Assert.Equal(id, pasajero.Id);
    }

    [Fact]
    public void CambiarClienteEmpresarialActualizaElCliente()
    {
        var pasajero = Pasajero.Crear(UsuarioPasajero(), Cliente);
        var otro = Guid.CreateVersion7();

        pasajero.CambiarClienteEmpresarial(otro);

        Assert.Equal(otro, pasajero.ClienteEmpresarialId);
    }

    [Fact]
    public void CambiarAUnClienteVacioFalla()
    {
        var pasajero = Pasajero.Crear(UsuarioPasajero(), Cliente);

        Assert.Throws<DomainException>(() => pasajero.CambiarClienteEmpresarial(Guid.Empty));
    }
}