using FlowRide.Dominio.Usuarios;
using FlowRide.Transversal.Excepciones;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class VehiculoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 14);
    private static readonly DateOnly Vence = new(2027, 6, 30);

    private static Vehiculo VehiculoConTodosLosDocumentos()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.RevisionTecnicoMecanica, "RTM-1", Vence));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.TarjetaOperacion, "TO-1", Vence));

        return vehiculo;
    }

    [Fact]
    public void CrearNormalizaLaPlacaYQuedaHabilitado()
    {
        var vehiculo = Vehiculo.Crear(" abc-123 ", 4);

        Assert.Equal("ABC123", vehiculo.Placa);
        Assert.Equal(4, vehiculo.Capacidad);
        Assert.True(vehiculo.Habilitado);
        Assert.NotEqual(Guid.Empty, vehiculo.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("ABCD12")]
    [InlineData("AB1234")]
    public void CrearConPlacaInvalidaFalla(string placa)
    {
        Assert.Throws<DomainException>(() => Vehiculo.Crear(placa, 1));
    }

    [Fact]
    public void AceptaPlacaDeMoto()
    {
        Assert.Equal("ABC12D", Vehiculo.Crear("ABC12D", 1).Placa);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CrearConCapacidadMenorAUnoFalla(int capacidad)
    {
        Assert.Throws<DomainException>(() => Vehiculo.Crear("ABC123", capacidad));
    }

    [Fact]
    public void CrearConIdLoRespeta()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id, Vehiculo.Crear("ABC123", 1, id).Id);
    }

    [Fact]
    public void NoAceptaDocumentosQueNoSonDeVehiculo()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        var licencia = DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, "LIC-1", Vence);

        Assert.Throws<DomainException>(() => vehiculo.RegistrarDocumento(licencia));
    }

    [Fact]
    public void RegistrarUnDocumentoDelMismoTipoLoReemplaza()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Hoy));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-2", Vence));

        Assert.Single(vehiculo.Documentos);
        Assert.Equal("SOAT-2", vehiculo.DocumentoDe(TipoDocumento.Soat)!.Numero);
    }

    [Fact]
    public void ConTodosLosDocumentosVigentesEsAsignable()
    {
        var vehiculo = VehiculoConTodosLosDocumentos();

        Assert.True(vehiculo.EsAsignableEn(Hoy));
        Assert.Empty(vehiculo.DocumentosNoVigentesEn(Hoy));
    }

    [Fact]
    public void UnDocumentoFaltanteLoVuelveNoAsignable()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence));

        var faltantes = vehiculo.DocumentosNoVigentesEn(Hoy);

        Assert.False(vehiculo.EsAsignableEn(Hoy));
        Assert.Contains(TipoDocumento.RevisionTecnicoMecanica, faltantes);
        Assert.Contains(TipoDocumento.TarjetaOperacion, faltantes);
    }

    [Fact]
    public void ElDocumentoSeEvaluaEnLaFechaDelServicio()
    {
        var vehiculo = Vehiculo.Crear("ABC123", 1);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Hoy.AddDays(2)));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.RevisionTecnicoMecanica, "RTM-1", Vence));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.TarjetaOperacion, "TO-1", Vence));

        Assert.True(vehiculo.EsAsignableEn(Hoy));
        Assert.False(vehiculo.EsAsignableEn(Hoy.AddDays(3)));
    }

    [Fact]
    public void DeshabilitarLoVuelveNoAsignableAunqueTengaDocumentos()
    {
        var vehiculo = VehiculoConTodosLosDocumentos();

        vehiculo.Deshabilitar();

        Assert.False(vehiculo.EsAsignableEn(Hoy));

        vehiculo.Habilitar();

        Assert.True(vehiculo.EsAsignableEn(Hoy));
    }
}
