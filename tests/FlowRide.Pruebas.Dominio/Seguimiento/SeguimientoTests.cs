using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Contratos;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Viajes.Dominio;

namespace FlowRide.Pruebas.Dominio.SeguimientoGps;

public class SeguimientoTests
{
    private static RegistroUbicacion Punto(double lat, double lon, DateTimeOffset origen, int ventana = 15) =>
        RegistroUbicacion.Crear(Id(1), null, null, origen, origen.AddSeconds(4), lat, lon, CalidadUbicacion.Continua, "Proveedor-GPS", ventana);

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    public void LaCoordenadaFueraDeRangoFalla(double lat, double lon)
    {
        Assert.Throws<DomainException>(() => Coordenada.Crear(lat, lon));
    }

    [Fact]
    public void LaDistanciaEntreBogotaYMedellinEsCoherente()
    {
        var bogota = Coordenada.Crear(4.7110, -74.0721);
        var medellin = Coordenada.Crear(6.2442, -75.5812);

        var km = bogota.DistanciaKm(medellin);

        Assert.InRange(km, 235, 250);
        Assert.Equal(0, bogota.DistanciaKm(bogota), 6);
    }

    [Fact]
    public void RegistrarConservaOrigenYRecepcionYCalculaElRetraso()
    {
        var registro = Punto(6.2, -75.5, Ahora);

        Assert.Equal(TimeSpan.FromSeconds(4), registro.RetrasoIngesta);
        Assert.Equal("proveedor-gps", registro.Fuente);
        Assert.Equal(6.2, registro.Posicion.Latitud);
    }

    [Fact]
    public void RechazaEventosFueraDeLaVentanaYDelFuturo()
    {
        Assert.Throws<DomainException>(() =>
            RegistroUbicacion.Crear(Id(1), null, null, Ahora.AddMinutes(-20), Ahora, 6.2, -75.5, CalidadUbicacion.Continua, "x", 15));
        Assert.Throws<DomainException>(() =>
            RegistroUbicacion.Crear(Id(1), null, null, Ahora.AddMinutes(10), Ahora, 6.2, -75.5, CalidadUbicacion.Puntual, "x", 15));
    }

    [Fact]
    public void ExigeVehiculoYFuente()
    {
        Assert.Throws<DomainException>(() =>
            RegistroUbicacion.Crear(Guid.Empty, null, null, Ahora, Ahora, 6.2, -75.5, CalidadUbicacion.Puntual, "x", 15));
        Assert.Throws<DomainException>(() =>
            RegistroUbicacion.Crear(Id(1), null, null, Ahora, Ahora, 6.2, -75.5, CalidadUbicacion.Puntual, " ", 15));
    }

    [Fact]
    public void ElEstimadorNecesitaAlMenosDosPuntos()
    {
        var destino = Coordenada.Crear(6.25, -75.56);

        Assert.Null(EstimadorEta.EstimarLlegada([Punto(6.2, -75.5, Ahora)], destino));
        Assert.Null(EstimadorEta.EstimarLlegada([Punto(6.2, -75.5, Ahora), Punto(6.21, -75.5, Ahora)], destino));
    }

    [Fact]
    public void ElEstimadorUsaLaVelocidadObservada()
    {
        var destino = Coordenada.Crear(6.40, -75.50);
        var a = Punto(6.20, -75.50, Ahora);
        var b = Punto(6.22, -75.50, Ahora.AddMinutes(4));

        var llegada = EstimadorEta.EstimarLlegada([a, b], destino);

        Assert.NotNull(llegada);
        var minutos = (llegada!.Value - b.TimestampOrigen).TotalMinutes;
        Assert.InRange(minutos, 30, 42);
    }

    [Fact]
    public void ElEstimadorAcotaLaVelocidadMinima()
    {
        var destino = Coordenada.Crear(6.30, -75.50);
        var a = Punto(6.20, -75.50, Ahora);
        var b = Punto(6.2001, -75.50, Ahora.AddMinutes(5));

        var llegada = EstimadorEta.EstimarLlegada([a, b], destino);

        Assert.True((llegada!.Value - b.TimestampOrigen).TotalHours < 5);
    }
}
