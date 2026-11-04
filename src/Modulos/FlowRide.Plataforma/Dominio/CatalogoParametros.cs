using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;

namespace FlowRide.Plataforma.Dominio;

public sealed record DefinicionParametro(string Clave, string Descripcion, int Minimo, int Maximo, int Defecto, string Unidad);

public static class CatalogoParametros
{
    public const string UmbralEtaMinutos = "eta.umbral_minutos";
    public const string AnticipacionRecordatorioMinutos = "recordatorio.anticipacion_minutos";
    public const string VentanaCancelacionMinutos = "solicitud.ventana_cancelacion_minutos";
    public const string AlertaSinConductorMinutos = "alerta.sin_conductor_minutos";
    public const string DiasAvisoDocumentos = "documentos.dias_aviso";
    public const string VentanaEventoGpsMinutos = "telemetria.ventana_evento_minutos";
    public const string RetencionTelemetriaMeses = "telemetria.retencion_meses";
    public const string VentanaCargaHoras ="asignacion.ventana_reciente_horas";
    public const string MaximoIntentosAsignacion = "asignacion.maximo_intentos";
    public const string DuracionViajeMinutos = "viaje.duracion_estimada_minutos";
    public const string AnticipacionAsignacionHoras = "asignacion.anticipacion_horas";

    private static readonly DefinicionParametro[] Todas =
    [
        new(UmbralEtaMinutos, "parametro.eta.umbral_minutos", 1, 30, 5, "unidad.min"),
        new(AnticipacionRecordatorioMinutos, "parametro.recordatorio.anticipacion_minutos", 5, 240, 30, "unidad.min"),
        new(VentanaCancelacionMinutos, "parametro.solicitud.ventana_cancelacion_minutos", 0, 1440, 60, "unidad.min"),
        new(AlertaSinConductorMinutos, "parametro.alerta.sin_conductor_minutos", 1, 120, 10, "unidad.min"),
        new(DiasAvisoDocumentos, "parametro.documentos.dias_aviso", 1, 180, 30, "unidad.dias"),
        new(VentanaEventoGpsMinutos, "parametro.telemetria.ventana_evento_minutos", 1, 120, 15, "unidad.min"),
        new(RetencionTelemetriaMeses, "parametro.telemetria.retencion_meses", 1, 24, 6, "unidad.meses"),
        new(VentanaCargaHoras, "parametro.asignacion.ventana_reciente_horas", 1, 72, 24, "unidad.h"),
        new(MaximoIntentosAsignacion, "parametro.asignacion.maximo_intentos", 1, 10, 3, "unidad.intentos"),
        new(DuracionViajeMinutos, "parametro.viaje.duracion_estimada_minutos", 5, 720, 45, "unidad.min"),
        new(AnticipacionAsignacionHoras, "parametro.asignacion.anticipacion_horas", 1, 720, 24, "unidad.h"),
    ];

    public static IReadOnlyList<DefinicionParametro> Definiciones => Todas;

    public static DefinicionParametro Obtener(string clave) =>
        Array.Find(Todas, d => d.Clave == clave)
        ?? throw new DomainException("error.el_parametro_no_existe", ("clave", clave));
}
