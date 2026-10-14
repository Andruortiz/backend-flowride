using FlowRide.Dominio.Usuarios;
using FlowRide.Transversal.Excepciones;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    private static Usuario NuevoPasajero() =>
        Usuario.Crear("Ana Pérez", null, "+57 300 123 4567", "P-001", RolUsuario.Pasajero, Ahora);

    [Fact]
    public void CrearPasajeroConTelefonoValidoQuedaActivoYNormalizado()
    {
        var usuario = NuevoPasajero();

        Assert.True(usuario.Activo);
        Assert.Equal("+573001234567", usuario.Telefono);
        Assert.Equal(Ahora, usuario.CreadoEn);
    }

    [Fact]
    public void CrearPasajeroSinTelefonoFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Ana", "ana@empresa.com", null, "P-002", RolUsuario.Pasajero, Ahora));
    }

    [Fact]
    public void CrearAdministradorSinCorreoFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Luis", null, "+573001234567", "A-001", RolUsuario.AdministradorOperador, Ahora));
    }

    [Fact]
    public void CrearConTelefonoSinPrefijoInternacionalFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Ana", null, "3001234567", "P-003", RolUsuario.Pasajero, Ahora));
    }

    [Fact]
    public void CorreoSeNormalizaAMinusculas()
    {
        var usuario = Usuario.Crear(
            "Luis", "  Luis@Empresa.COM ", null, "A-002", RolUsuario.AdministradorOperador, Ahora);

        Assert.Equal("luis@empresa.com", usuario.Correo);
    }

    [Fact]
    public void NombreSoloConEspaciosFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("   ", null, "+573001234567", "P-006", RolUsuario.Pasajero, Ahora));
    }

    [Fact]
    public void CrearSinIdGeneraUnoNuevoCadaVez()
    {
        var a = NuevoPasajero();
        var b = NuevoPasajero();

        Assert.NotEqual(Guid.Empty, a.Id);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void CrearConIdLoRespeta()
    {
        var id = Guid.CreateVersion7();

        var usuario = Usuario.Crear("Ana", null, "+573001234567", "P-004", RolUsuario.Pasajero, Ahora, id);

        Assert.Equal(id, usuario.Id);
    }

    [Fact]
    public void CrearConIdVacioGeneraUnoNuevo()
    {
        var usuario = Usuario.Crear("Ana", null, "+573001234567", "P-005", RolUsuario.Pasajero, Ahora, Guid.Empty);

        Assert.NotEqual(Guid.Empty, usuario.Id);
    }

    [Fact]
    public void DesactivarRegistraInstanteYEsIdempotente()
    {
        var usuario = NuevoPasajero();
        var despues = Ahora.AddHours(1);

        usuario.Desactivar(Ahora);
        usuario.Desactivar(despues);

        Assert.False(usuario.Activo);
        Assert.Equal(Ahora, usuario.DesactivadoEn);
    }

    [Fact]
    public void ActivarRestauraElUsuario()
    {
        var usuario = NuevoPasajero();
        usuario.Desactivar(Ahora);

        usuario.Activar();

        Assert.True(usuario.Activo);
        Assert.Null(usuario.DesactivadoEn);
    }

    [Fact]
    public void CambiarRolSinElContactoRequeridoFalla()
    {
        var usuario = Usuario.Crear("Pedro", null, "+573001234567", "C-002", RolUsuario.Conductor, Ahora);

        Assert.Throws<DomainException>(() => usuario.CambiarRol(RolUsuario.OperadorLogistico));
    }

    [Fact]
    public void CambiarRolConContactoRequeridoFunciona()
    {
        var usuario = Usuario.Crear("Pedro", "pedro@op.com", "+573001234567", "C-003", RolUsuario.Conductor, Ahora);

        usuario.CambiarRol(RolUsuario.OperadorLogistico);

        Assert.Equal(RolUsuario.OperadorLogistico, usuario.Rol);
    }

    [Fact]
    public void VincularOtraIdentidadExternaFalla()
    {
        var usuario = NuevoPasajero();
        usuario.VincularIdentidadExterna("kc-123");

        Assert.Throws<DomainException>(() => usuario.VincularIdentidadExterna("kc-999"));
    }
}
