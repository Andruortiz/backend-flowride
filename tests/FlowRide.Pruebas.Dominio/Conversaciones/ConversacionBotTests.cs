using static FlowRide.Pruebas.Dominio.Escenarios;

using FlowRide.Comunicacion.Dominio;
using FlowRide.Contratos;


namespace FlowRide.Pruebas.Dominio.Conversaciones;

public class ConversacionBotTests
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    private static ConversacionBot Nueva() => ConversacionBot.Iniciar(Id(1), Ahora);

    private static RespuestaConversacion Di(ConversacionBot bot, string texto, int minutos = 0) =>
        bot.Procesar(texto, Ahora.AddMinutes(minutos), Bogota);

    [Fact]
    public void SinComandoMuestraElMenu()
    {
        var respuesta = Di(Nueva(), "buenas");

        Assert.Contains("SOLICITAR", respuesta.Texto);
        Assert.Equal(IntencionConversacion.Ninguna, respuesta.Intencion);
    }

    [Fact]
    public void ElFlujoCompletoProduceLosDatosDeLaSolicitudEnUtc()
    {
        var bot = Nueva();

        Di(bot, "solicitar");
        Di(bot, "Aeropuerto JMC");
        Di(bot, "Hotel Poblado");
        Di(bot, "2026-10-15 08:30");
        var resumen = Di(bot, "sí");
        var final = Di(bot, "confirmar");

        Assert.Contains("Confirma tu solicitud", resumen.Texto);
        Assert.Equal(IntencionConversacion.SolicitudCompleta, final.Intencion);
        Assert.Equal("Aeropuerto JMC", final.Solicitud!.Origen);
        Assert.Equal("Hotel Poblado", final.Solicitud.Destino);
        Assert.True(final.Solicitud.Urgente);
        Assert.Equal(new DateTimeOffset(2026, 10, 15, 13, 30, 0, TimeSpan.Zero), final.Solicitud.FechaHoraUtc);
        Assert.Equal(EstadoConversacion.Completa, bot.Estado);
    }

    [Fact]
    public void RechazaDestinoIgualFechaInvalidaFechaPasadaYUrgenciaConfusa()
    {
        var bot = Nueva();
        Di(bot, "solicitar");
        Di(bot, "Casa");

        Assert.Contains("distinto", Di(bot, "casa").Texto);
        Di(bot, "Oficina");

        Assert.Contains("formato", Di(bot, "mañana").Texto);
        Assert.Contains("ya pasó", Di(bot, "2020-01-01 08:00").Texto);

        Di(bot, "14/10/2026 18:00");

        Assert.Contains("SÍ o NO", Di(bot, "quizás").Texto);
        Assert.Equal(EstadoConversacion.EsperandoUrgencia, bot.Estado);
    }

    [Fact]
    public void CancelarReiniciaYEstadoEsUnaIntencion()
    {
        var bot = Nueva();
        Di(bot, "solicitar");
        Di(bot, "Casa");

        Assert.Equal(IntencionConversacion.ConsultarEstado, Di(bot, "ESTADO").Intencion);
        Assert.Equal(EstadoConversacion.EsperandoDestino, bot.Estado);

        Di(bot, "cancelar");

        Assert.Equal(EstadoConversacion.Inicio, bot.Estado);
        Assert.Null(bot.Origen);
    }

    [Fact]
    public void LaConversacionInactivaVenceYVuelveAlInicio()
    {
        var bot = Nueva();
        Di(bot, "solicitar");
        Di(bot, "Casa");

        var respuesta = Di(bot, "Oficina", minutos: 45);

        Assert.Equal(EstadoConversacion.Inicio, bot.Estado);
        Assert.Contains("SOLICITAR", respuesta.Texto);
    }

    [Fact]
    public void ConfirmarExigeUnaRespuestaAfirmativa()
    {
        var bot = Nueva();
        Di(bot, "solicitar");
        Di(bot, "Casa");
        Di(bot, "Oficina");
        Di(bot, "2026-10-15 08:30");
        Di(bot, "no");

        var duda = Di(bot, "tal vez");

        Assert.Equal(IntencionConversacion.Ninguna, duda.Intencion);
        Assert.Equal(EstadoConversacion.EsperandoConfirmacion, bot.Estado);
    }
}
