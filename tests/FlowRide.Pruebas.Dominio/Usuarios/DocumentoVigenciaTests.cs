using FlowRide.Dominio.Usuarios;
using FlowRide.Transversal.Excepciones;

namespace FlowRide.Pruebas.Dominio.Usuarios;

public class DocumentoVigenciaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 14);
    private static readonly DateOnly Vence = new(2026, 12, 31);

    private static DocumentoVigencia Soat(DateOnly venceEl) =>
        DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-123", venceEl);

    [Fact]
    public void CrearConDatosValidosConservaTipoNumeroYVencimiento()
    {
        var doc = DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, "LIC-77", Vence);

        Assert.Equal(TipoDocumento.LicenciaConduccion, doc.Tipo);
        Assert.Equal("LIC-77", doc.Numero);
        Assert.Equal(Vence, doc.VenceEl);
    }

    [Fact]
    public void ElNumeroSeNormalizaSinEspaciosLaterales()
    {
        var doc = DocumentoVigencia.Crear(TipoDocumento.Soat, "  SOAT-9  ", Vence);

        Assert.Equal("SOAT-9", doc.Numero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CrearSinNumeroFalla(string? numero)
    {
        Assert.Throws<DomainException>(() =>
            DocumentoVigencia.Crear(TipoDocumento.Soat, numero!, Vence));
    }

    [Fact]
    public void CrearSinFechaDeVencimientoFalla()
    {
        Assert.Throws<DomainException>(() =>
            DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", default));
    }

    [Fact]
    public void CrearConTipoInexistenteFalla()
    {
        Assert.Throws<DomainException>(() =>
            DocumentoVigencia.Crear((TipoDocumento)99, "X-1", Vence));
    }

    [Fact]
    public void SigueVigenteElMismoDiaDelVencimiento()
    {
        var doc = Soat(Hoy);

        Assert.True(doc.EstaVigenteEn(Hoy));
        Assert.Equal(0, doc.DiasParaVencer(Hoy));
    }

    [Fact]
    public void QuedaVencidoAlDiaSiguiente()
    {
        var doc = Soat(Hoy);
        var manana = Hoy.AddDays(1);

        Assert.False(doc.EstaVigenteEn(manana));
        Assert.Equal(EstadoVigencia.Vencido, doc.EstadoEn(manana));
        Assert.Equal(-1, doc.DiasParaVencer(manana));
    }

    [Fact]
    public void EstaPorVencerCuandoFaltanTreintaDiasOMenos()
    {
        var doc = Soat(Hoy.AddDays(30));

        Assert.Equal(EstadoVigencia.PorVencer, doc.EstadoEn(Hoy));
    }

    [Fact]
    public void EstaVigenteCuandoFaltanMasDeTreintaDias()
    {
        var doc = Soat(Hoy.AddDays(31));

        Assert.Equal(EstadoVigencia.Vigente, doc.EstadoEn(Hoy));
    }

    [Fact]
    public void ElAvisoPuedeAjustarseSegunElOperador()
    {
        var doc = Soat(Hoy.AddDays(45));

        Assert.Equal(EstadoVigencia.Vigente, doc.EstadoEn(Hoy));
        Assert.Equal(EstadoVigencia.PorVencer, doc.EstadoEn(Hoy, diasAviso: 60));
    }

    [Fact]
    public void DosDocumentosConLosMismosDatosSonIguales()
    {
        var a = DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence);
        var b = DocumentoVigencia.Crear(TipoDocumento.Soat, "SOAT-1", Vence);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void RenovarConFechaPosteriorDevuelveUnDocumentoNuevoDelMismoTipo()
    {
        var original = Soat(Hoy);

        var renovado = original.Renovar("SOAT-2", Hoy.AddYears(1));

        Assert.Equal(TipoDocumento.Soat, renovado.Tipo);
        Assert.Equal("SOAT-2", renovado.Numero);
        Assert.Equal(Hoy.AddYears(1), renovado.VenceEl);
        Assert.Equal(Hoy, original.VenceEl);
    }

    [Fact]
    public void RenovarConFechaNoPosteriorFalla()
    {
        var doc = Soat(Hoy);

        Assert.Throws<DomainException>(() => doc.Renovar("SOAT-2", Hoy));
    }
}