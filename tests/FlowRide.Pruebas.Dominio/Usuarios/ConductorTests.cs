using FlowRide.Dominio.Usuarios;
using FlowRide.Transversal.Excepciones;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class ConductorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 10, 14);
    private static readonly TimeOnly Hora = new(8, 30);
    private static readonly DateOnly Vence = new(2027, 6, 30);

    private static Usuario UsuarioConductor() =>
        Usuario.Crear("Pedro Gil", null, "+573001234567", "C-001", RolUsuario.Conductor, Ahora);

    private static DocumentoVigencia Licencia(DateOnly venceEl) =>
        DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, "LIC-1", venceEl);

    private static Vehiculo VehiculoAlDia()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.RevisionTecnicoMecanica, "RTM-1", Vence));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.TarjetaOperacion, "TO-1", Vence));

        return vehiculo;
    }

    private static Conductor ConductorListo()
    {
        var conductor = Conductor.Crear(UsuarioConductor(), Licencia(Vence));
        conductor.DefinirTurnos([Turno.Crear(Hoy.DayOfWeek, new TimeOnly(5, 0), new TimeOnly(13, 0))]);

        return conductor;
    }

    [Fact]
    public void CrearReferenciaAlUsuarioPorIdYQuedaActivoYDisponible()
    {
        var usuario = UsuarioConductor();

        var conductor = Conductor.Crear(usuario, Licencia(Vence));

        Assert.Equal(usuario.Id, conductor.UsuarioId);
        Assert.True(conductor.Activo);
        Assert.True(conductor.Disponible);
        Assert.Null(conductor.VehiculoId);
    }

    [Fact]
    public void CrearConUsuarioQueNoEsConductorFalla()
    {
        var pasajero = Usuario.Crear("Ana", null, "+573001234567", "P-001", RolUsuario.Pasajero, Ahora);

        Assert.Throws<DomainException>(() => Conductor.Crear(pasajero, Licencia(Vence)));
    }

    [Fact]
    public void CrearConUnDocumentoQueNoEsLicenciaFalla()
    {
        var soat = DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence);

        Assert.Throws<DomainException>(() => Conductor.Crear(UsuarioConductor(), soat));
    }

    [Fact]
    public void CrearConIdLoRespeta()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, Conductor.Crear(UsuarioConductor(), Licencia(Vence), id).Id);
    }

    [Fact]
    public void AsignarVehiculoGuardaSoloSuIdentificador()
    {
        var conductor = Conductor.Crear(UsuarioConductor(), Licencia(Vence));
        var vehiculo = VehiculoAlDia();

        conductor.AsignarVehiculo(vehiculo);

        Assert.Equal(vehiculo.Id, conductor.VehiculoId);

        conductor.QuitarVehiculo();

        Assert.Null(conductor.VehiculoId);
    }

    [Fact]
    public void MarcarNoDisponibleExigeMotivoYLoConserva()
    {
        var conductor = Conductor.Crear(UsuarioConductor(), Licencia(Vence));

        Assert.Throws<DomainException>(() => conductor.MarcarNoDisponible("  "));

        conductor.MarcarNoDisponible("  Vehículo en taller ");

        Assert.False(conductor.Disponible);
        Assert.Equal("Vehículo en taller", conductor.MotivoNoDisponible);

        conductor.MarcarDisponible();

        Assert.True(conductor.Disponible);
        Assert.Null(conductor.MotivoNoDisponible);
    }

    [Fact]
    public void DefinirTurnosQueSeSolapanFalla()
    {
        var conductor = Conductor.Crear(UsuarioConductor(), Licencia(Vence));
        var a = Turno.Crear(DayOfWeek.Monday, new TimeOnly(5, 0), new TimeOnly(13, 0));
        var b = Turno.Crear(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(20, 0));

        Assert.Throws<DomainException>(() => conductor.DefinirTurnos([a, b]));
        Assert.Empty(conductor.Turnos);
    }

    [Fact]
    public void DefinirTurnosReemplazaLosAnteriores()
    {
        var conductor = ConductorListo();
        var nuevo = Turno.Crear(DayOfWeek.Friday, new TimeOnly(13, 0), new TimeOnly(21, 0));

        conductor.DefinirTurnos([nuevo]);

        Assert.Equal([nuevo], conductor.Turnos);
    }

    [Fact]
    public void ConductorCompletoEnTurnoYConVehiculoAlDiaEsAsignable()
    {
        var evaluacion = ConductorListo().EvaluarAsignabilidad(VehiculoAlDia(), Hoy, Hora);

        Assert.True(evaluacion.EsAsignable);
        Assert.Empty(evaluacion.Motivos);
    }

    [Fact]
    public void FueraDeTurnoNoEsAsignable()
    {
        var evaluacion = ConductorListo().EvaluarAsignabilidad(VehiculoAlDia(), Hoy, new TimeOnly(15, 0));

        Assert.Equal([MotivoNoAsignable.FueraDeTurno], evaluacion.Motivos);
    }

    [Fact]
    public void SinVehiculoNoEsAsignable()
    {
        var evaluacion = ConductorListo().EvaluarAsignabilidad(null, Hoy, Hora);

        Assert.Contains(MotivoNoAsignable.SinVehiculo, evaluacion.Motivos);
    }

    [Fact]
    public void LicenciaVencidaEnLaFechaDelServicioNoEsAsignable()
    {
        var conductor = Conductor.Crear(UsuarioConductor(), Licencia(Hoy));
        conductor.DefinirTurnos(
        [
            Turno.Crear(Hoy.DayOfWeek, new TimeOnly(5, 0), new TimeOnly(13, 0)),
            Turno.Crear(Hoy.AddDays(1).DayOfWeek, new TimeOnly(5, 0), new TimeOnly(13, 0)),
        ]);
        var vehiculo = VehiculoAlDia();

        Assert.True(conductor.EvaluarAsignabilidad(vehiculo, Hoy, Hora).EsAsignable);
        Assert.Contains(
            MotivoNoAsignable.LicenciaNoVigente,
            conductor.EvaluarAsignabilidad(vehiculo, Hoy.AddDays(1), Hora).Motivos);
    }

    [Fact]
    public void InactivoYNoDisponibleAcumulanSusMotivos()
    {
        var conductor = ConductorListo();
        conductor.Inactivar();
        conductor.MarcarNoDisponible("Descanso");

        var evaluacion = conductor.EvaluarAsignabilidad(VehiculoAlDia(), Hoy, Hora);

        Assert.Equal(
            [MotivoNoAsignable.ConductorInactivo, MotivoNoAsignable.ConductorNoDisponible],
            evaluacion.Motivos);
    }

    [Fact]
    public void VehiculoDeshabilitadoOConDocumentosVencidosNoEsAsignable()
    {
        var conductor = ConductorListo();

        var deshabilitado = VehiculoAlDia();
        deshabilitado.Deshabilitar();

        var sinDocumentos = Vehiculo.Crear("XYZ987", 1);

        Assert.Contains(
            MotivoNoAsignable.VehiculoNoHabilitado,
            conductor.EvaluarAsignabilidad(deshabilitado, Hoy, Hora).Motivos);
        Assert.Contains(
            MotivoNoAsignable.DocumentosVehiculoNoVigentes,
            conductor.EvaluarAsignabilidad(sinDocumentos, Hoy, Hora).Motivos);
    }
}
