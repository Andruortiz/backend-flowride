using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Clientes.Dominio;
using FlowRide.Contratos;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Plataforma.Dominio;
using FlowRide.Solicitudes.Dominio;


namespace FlowRide.Pruebas.Dominio.Solicitudes;

public class SolicitudTransporteTests
{
    [Fact]
    public void RegistrarQuedaPendienteYEmiteElEvento()
    {
        var solicitud = Solicitud(urgente: true);

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        var evento = Assert.IsType<SolicitudRegistrada>(Assert.Single(solicitud.EventosDominio));
        Assert.Equal(solicitud.Id, evento.SolicitudId);
        Assert.True(evento.Urgente);
    }

    [Fact]
    public void RegistrarConFechaPasadaFalla()
    {
        Assert.Throws<DomainException>(() => Solicitud(cuando: Ahora.AddMinutes(-1)));
    }

    [Fact]
    public void RegistrarConOrigenIgualAlDestinoFalla()
    {
        Assert.Throws<DomainException>(() =>
            SolicitudTransporte.Registrar(NuevoPasajero(), "Casa", " casa ", Ahora.AddHours(1), false, false, Ahora));
    }

    [Fact]
    public void MarcarAsignadaGuardaLaAsignacion()
    {
        var solicitud = Solicitud();
        var asignacion = Id(1);

        solicitud.MarcarAsignada(asignacion);

        Assert.Equal(EstadoSolicitud.Asignada, solicitud.Estado);
        Assert.Equal(asignacion, solicitud.AsignacionId);
    }

    [Fact]
    public void NoAdmiteUnaSegundaAsignacion()
    {
        var solicitud = Solicitud();
        solicitud.MarcarAsignada(Id(1));

        Assert.Throws<DomainException>(() => solicitud.MarcarAsignada(Id(2)));
    }

    [Fact]
    public void VolverAPendienteCuentaElIntento()
    {
        var solicitud = Solicitud();
        solicitud.MarcarAsignada(Id(1));

        solicitud.VolverAPendiente();

        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.AsignacionId);
        Assert.Equal(1, solicitud.Intentos);
    }

    [Fact]
    public void MarcarEnRevisionEmiteSinRecursoDisponible()
    {
        var solicitud = Solicitud();
        solicitud.LimpiarEventos();

        solicitud.MarcarEnRevision("Sin candidatos", Ahora);

        Assert.Equal(EstadoSolicitud.EnRevision, solicitud.Estado);
        Assert.IsType<SinRecursoDisponible>(Assert.Single(solicitud.EventosDominio));
    }

    [Fact]
    public void ElPasajeroNoCancelaFueraDeLaVentana()
    {
        var solicitud = Solicitud(cuando: Ahora.AddMinutes(30));

        Assert.Throws<DomainException>(() => solicitud.Cancelar("Ya no voy", esOperador: false, ventanaMinutos: 60, Ahora));
    }

    [Fact]
    public void ElOperadorCancelaSinVentanaYElEventoLlevaLaAsignacion()
    {
        var solicitud = Solicitud(cuando: Ahora.AddMinutes(30));
        solicitud.MarcarAsignada(Id(7));
        solicitud.LimpiarEventos();

        solicitud.Cancelar("Contingencia", esOperador: true, ventanaMinutos: 60, Ahora);

        Assert.Equal(EstadoSolicitud.Cancelada, solicitud.Estado);
        var evento = Assert.IsType<SolicitudCancelada>(Assert.Single(solicitud.EventosDominio));
        Assert.Equal(Id(7), evento.AsignacionId);
    }

    [Fact]
    public void CancelarExigeMotivoYNoSeCancelaDosVeces()
    {
        var solicitud = Solicitud();

        Assert.Throws<DomainException>(() => solicitud.Cancelar(" ", true, 60, Ahora));

        solicitud.Cancelar("Cambio de planes", true, 60, Ahora);

        Assert.Throws<DomainException>(() => solicitud.Cancelar("Otra vez", true, 60, Ahora));
    }

    [Fact]
    public void ModificarSoloSiNoTieneConductor()
    {
        var solicitud = Solicitud();

        solicitud.Modificar("Terminal", "Centro", Ahora.AddHours(8), Ahora);

        Assert.Equal("Terminal", solicitud.Origen);

        solicitud.MarcarAsignada(Id(1));

        Assert.Throws<DomainException>(() => solicitud.Modificar("A", "B", Ahora.AddHours(9), Ahora));
    }

    [Fact]
    public void IniciarYFinalizarSiguenElOrden()
    {
        var solicitud = Solicitud();

        Assert.Throws<DomainException>(solicitud.IniciarServicio);

        solicitud.MarcarAsignada(Id(1));
        solicitud.IniciarServicio();
        solicitud.Finalizar();

        Assert.Equal(EstadoSolicitud.Finalizada, solicitud.Estado);
    }

    [Fact]
    public void ElRecordatorioSeEmiteUnaSolaVezYSoloSiEstaAsignada()
    {
        var solicitud = Solicitud();
        solicitud.LimpiarEventos();

        solicitud.RegistrarRecordatorioDebido(Ahora);
        Assert.Empty(solicitud.EventosDominio);

        solicitud.MarcarAsignada(Id(1));
        solicitud.RegistrarRecordatorioDebido(Ahora);
        solicitud.RegistrarRecordatorioDebido(Ahora);

        Assert.IsType<RecordatorioViajeDebido>(Assert.Single(solicitud.EventosDominio));
    }

    [Fact]
    public void ItinerarioExigeCamposYHorasDistintas()
    {
        var ok = Itinerario.Crear("SRV-1", null, "A", "B", Fecha, new TimeOnly(7, 0), new TimeOnly(8, 0), Ahora);

        Assert.Equal("SRV-1", ok.CodigoServicio);
        Assert.Throws<DomainException>(() =>
            Itinerario.Crear("SRV-1", null, "A", "B", Fecha, new TimeOnly(7, 0), new TimeOnly(7, 0), Ahora));
        Assert.Throws<DomainException>(() =>
            Itinerario.Crear(" ", null, "A", "B", Fecha, new TimeOnly(7, 0), new TimeOnly(8, 0), Ahora));
    }

    [Fact]
    public void OperadorValidaZonaHorariaYCorreo()
    {
        var operador = Operador.Crear("TransAndes", "900123456-1", "Contacto@TransAndes.co", "America/Bogota", Ahora);

        Assert.Equal("contacto@transandes.co", operador.CorreoContacto);
        Assert.Equal(TimeSpan.FromHours(-5), operador.ObtenerZona().GetUtcOffset(Ahora));
        Assert.Throws<DomainException>(() => Operador.Crear("X", "1", "a@b.co", "Mars/Olympus", Ahora));
        Assert.Throws<DomainException>(() => Operador.Crear("X", "1", "no-es-correo", "America/Bogota", Ahora));
    }
}
