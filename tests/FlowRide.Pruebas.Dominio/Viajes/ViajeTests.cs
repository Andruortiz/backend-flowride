using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Contratos;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Viajes.Dominio;


namespace FlowRide.Pruebas.Dominio.Viajes;

public class ViajeTests
{
    private static Viaje NuevoViaje() =>
        Viaje.Crear(Id(1), Id(2), Id(3), Id(4), Id(5), Ahora.AddHours(2), Ahora.AddHours(3), Ahora);

    [Fact]
    public void CrearQuedaProgramadoConSuPrimerHito()
    {
        var viaje = NuevoViaje();

        Assert.Equal(EstadoViaje.Programado, viaje.Estado);
        Assert.Equal("Programado", Assert.Single(viaje.Hitos).Tipo);
    }

    [Fact]
    public void LaLlegadaDebeSerPosteriorALaSalida()
    {
        Assert.Throws<DomainException>(() =>
            Viaje.Crear(Id(1), Id(2), Id(3), Id(4), Id(5), Ahora.AddHours(2), Ahora.AddHours(2), Ahora));
    }

    [Fact]
    public void IniciarYFinalizarEmitenSusEventosYQuedanEnLaTrazabilidad()
    {
        var viaje = NuevoViaje();

        viaje.Iniciar(Ahora.AddHours(2));
        viaje.Finalizar(Ahora.AddHours(3));

        Assert.Equal(EstadoViaje.Finalizado, viaje.Estado);
        Assert.Contains(viaje.EventosDominio, e => e is ViajeIniciado);
        Assert.Contains(viaje.EventosDominio, e => e is ViajeFinalizado);
        Assert.Equal(["Programado", "Inicio", "Fin"], viaje.Hitos.Select(h => h.Tipo));
    }

    [Fact]
    public void NoSeIniciaDosVecesNiSeFinalizaSinIniciar()
    {
        var viaje = NuevoViaje();

        Assert.Throws<DomainException>(() => viaje.Finalizar(Ahora));

        viaje.Iniciar(Ahora);

        Assert.Throws<DomainException>(() => viaje.Iniciar(Ahora));
    }

    [Fact]
    public void ElIncidenteSoloSeReportaEnCursoYExigeDescripcion()
    {
        var viaje = NuevoViaje();

        Assert.Throws<DomainException>(() => viaje.ReportarIncidente("Pinchazo", Ahora));

        viaje.Iniciar(Ahora);

        Assert.Throws<DomainException>(() => viaje.ReportarIncidente(" ", Ahora));

        viaje.ReportarIncidente("Pinchazo", Ahora);

        Assert.Contains(viaje.EventosDominio, e => e is IncidenteReportado { Descripcion: "Pinchazo" });
    }

    [Fact]
    public void CancelarSoloAntesDeIniciar()
    {
        var viaje = NuevoViaje();
        viaje.Cancelar(Ahora);

        Assert.Equal(EstadoViaje.Cancelado, viaje.Estado);

        var otro = NuevoViaje();
        otro.Iniciar(Ahora);

        Assert.Throws<DomainException>(() => otro.Cancelar(Ahora));
    }

    [Fact]
    public void LaDesviacionDeEtaAlertaUnaVezYSeRearmaAlNormalizarse()
    {
        var viaje = NuevoViaje();
        viaje.Iniciar(Ahora);
        viaje.LimpiarEventos();

        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(3), 5, Ahora);
        Assert.Empty(viaje.EventosDominio);

        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(7), 5, Ahora);
        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(8), 5, Ahora);
        var alerta = Assert.IsType<DesviacionEtaDetectada>(Assert.Single(viaje.EventosDominio));
        Assert.Equal(7, alerta.DesvioMinutos);

        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(1), 5, Ahora);
        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(9), 5, Ahora);

        Assert.Equal(2, viaje.EventosDominio.OfType<DesviacionEtaDetectada>().Count());
    }

    [Fact]
    public void LaDesviacionSoloSeEvaluaConElViajeEnCurso()
    {
        var viaje = NuevoViaje();

        viaje.EvaluarDesviacionEta(viaje.EtaProgramada.AddMinutes(30), 5, Ahora);

        Assert.Empty(viaje.EventosDominio);
    }

    [Fact]
    public void CalificarExigeViajeFinalizadoDelMismoPasajeroYPuntajeValido()
    {
        var viaje = NuevoViaje();

        Assert.Throws<DomainException>(() => Calificacion.Crear(viaje, Id(3), 5, null, Ahora));

        viaje.Iniciar(Ahora);
        viaje.Finalizar(Ahora);

        Assert.Throws<DomainException>(() => Calificacion.Crear(viaje, Id(99), 5, null, Ahora));
        Assert.Throws<DomainException>(() => Calificacion.Crear(viaje, Id(3), 6, null, Ahora));
        Assert.Throws<DomainException>(() => Calificacion.Crear(viaje, Id(3), 0, null, Ahora));
        Assert.Throws<DomainException>(() => Calificacion.Crear(viaje, Id(3), 5, new string('x', 501), Ahora));

        var calificacion = Calificacion.Crear(viaje, Id(3), 4, "  Muy bien ", Ahora);

        Assert.Equal(viaje.Id, calificacion.ViajeId);
        Assert.Equal(viaje.ConductorId, calificacion.ConductorId);
        Assert.Equal("Muy bien", calificacion.Comentario);
    }
}
