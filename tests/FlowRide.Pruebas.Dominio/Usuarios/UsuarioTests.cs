using FlowRide.Dominio.Usuarios;
using FlowRide.Transversal.Excepciones;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);

    private static Usuario NuevoPasajero() =>
        Usuario.Crear("Ana Pérez", null, "+57 300 123 4567", "P-001", RolUsuario.Pasajero, Guid.NewGuid(), Ahora);

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
            Usuario.Crear("Ana", "ana@empresa.com", null, "P-002", RolUsuario.Pasajero, null, Ahora));
    }

    [Fact]
    public void CrearAdministradorSinCorreoFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Luis", null, "+573001234567", "A-001", RolUsuario.AdministradorOperador, null, Ahora));
    }

    [Fact]
    public void CrearConTelefonoSinPrefijoInternacionalFalla()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Ana", null, "3001234567", "P-003", RolUsuario.Pasajero, null, Ahora));
    }

    [Fact]
    public void CorreoSeNormalizaAMinusculas()
    {
        var usuario = Usuario.Crear(
            "Luis", "  Luis@Empresa.COM ", null, "A-002", RolUsuario.AdministradorOperador, null, Ahora);

        Assert.Equal("luis@empresa.com", usuario.Correo);
    }

    [Fact]
    public void ConductorNoPuedeAsociarseAClienteEmpresarial()
    {
        Assert.Throws<DomainException>(() =>
            Usuario.Crear("Pedro", null, "+573001234567", "C-001", RolUsuario.Conductor, Guid.NewGuid(), Ahora));
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
        var usuario = Usuario.Crear("Pedro", null, "+573001234567", "C-002", RolUsuario.Conductor, null, Ahora);

        Assert.Throws<DomainException>(() => usuario.CambiarRol(RolUsuario.OperadorLogistico));
    }

    [Fact]
    public void CambiarRolConContactoRequeridoFunciona()
    {
        var usuario = Usuario.Crear("Pedro", "pedro@op.com", "+573001234567", "C-003", RolUsuario.Conductor, null, Ahora);

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