using FlowRide.Nucleo.Excepciones;
using FlowRide.Operacion.Aplicacion;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using NSubstitute;

namespace FlowRide.Pruebas.Servicios;

public class ServicioReportesTests
{
    private static readonly DateTimeOffset Desde = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly DoblesComunes _comunes = new();
    private readonly IReportesQuery _consultas = Substitute.For<IReportesQuery>();
    private readonly ServicioReportes _servicio;

    public ServicioReportesTests()
    {
        _servicio = new ServicioReportes(_consultas, _comunes.Parametros);
    }

    [Fact]
    public async Task UnRangoInvertidoSeRechaza()
    {
        var error = await Assert.ThrowsAsync<DomainException>(() => _servicio.SolicitudesPorEstadoAsync(Desde, Desde.AddDays(-1)));

        Assert.Equal("error.el_rango_del_reporte_es_invalido_hasta", error.Clave);
    }

    [Fact]
    public async Task UnRangoMayorAUnAnoSeRechaza()
    {
        var error = await Assert.ThrowsAsync<DomainException>(() => _servicio.OperacionPorClienteAsync(Desde, Desde.AddDays(400)));

        Assert.Equal("error.el_rango_del_reporte_no_puede_superar", error.Clave);
    }

    [Fact]
    public async Task UnRangoValidConsultaCadaReporte()
    {
        var hasta = Desde.AddDays(30);
        var cliente = Guid.NewGuid();

        await _servicio.ViajesRealizadosAsync(Desde, hasta, cliente);
        await _servicio.SolicitudesPorEstadoAsync(Desde, hasta);
        await _servicio.ConductoresAsignadosAsync(Desde, hasta);
        await _servicio.OperacionPorClienteAsync(Desde, hasta);

        await _consultas.Received(1).ViajesRealizadosAsync(Desde, hasta, cliente, Arg.Any<CancellationToken>());
        await _consultas.Received(1).SolicitudesPorEstadoAsync(Desde, hasta, Arg.Any<CancellationToken>());
        await _consultas.Received(1).ConductoresAsignadosAsync(Desde, hasta, Arg.Any<CancellationToken>());
        await _consultas.Received(1).OperacionPorClienteAsync(Desde, hasta, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LosIndicadoresUsanElUmbralDeEtaConfigurado()
    {
        var hasta = Desde.AddDays(7);
        _comunes.Parametros.ValorAsync(CatalogoParametros.UmbralEtaMinutos, Arg.Any<CancellationToken>()).Returns(7);

        await _servicio.IndicadoresAsync(Desde, hasta);

        await _consultas.Received(1).IndicadoresAsync(Desde, hasta, 7, Arg.Any<CancellationToken>());
    }
}
