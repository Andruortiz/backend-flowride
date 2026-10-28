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

/// <summary>Parámetros operativos que cada operador puede ajustar sin desplegar (ADR-0016), con su rango válido.</summary>
public static class CatalogoParametros
{
    public const string UmbralEtaMinutos = "eta.umbral_minutos";
    public const string AnticipacionRecordatorioMinutos = "recordatorio.anticipacion_minutos";
    public const string VentanaCancelacionMinutos = "solicitud.ventana_cancelacion_minutos";
    public const string AlertaSinConductorMinutos = "alerta.sin_conductor_minutos";
    public const string DiasAvisoDocumentos = "documentos.dias_aviso";
    public const string VentanaEventoGpsMinutos = "telemetria.ventana_evento_minutos";
    public const string VentanaCargaHoras = "asignacion.ventana_reciente_horas";
    public const string MaximoIntentosAsignacion = "asignacion.maximo_intentos";
    public const string DuracionViajeMinutos = "viaje.duracion_estimada_minutos";

    private static readonly DefinicionParametro[] Todas =
    [
        new(UmbralEtaMinutos, "Desviación de ETA que genera alerta", 1, 30, 5, "min"),
        new(AnticipacionRecordatorioMinutos, "Anticipación del recordatorio de viaje", 5, 240, 30, "min"),
        new(VentanaCancelacionMinutos, "Antelación mínima para que el pasajero cancele", 0, 1440, 60, "min"),
        new(AlertaSinConductorMinutos, "Tiempo sin conductor antes de alertar al operador", 1, 120, 10, "min"),
        new(DiasAvisoDocumentos, "Días de aviso antes del vencimiento de un documento", 1, 180, 30, "días"),
        new(VentanaEventoGpsMinutos, "Antigüedad máxima aceptada de un evento GPS", 1, 120, 15, "min"),
        new(VentanaCargaHoras, "Ventana para contar asignaciones recientes", 1, 72, 24, "h"),
        new(MaximoIntentosAsignacion, "Intentos de asignación antes de pasar a revisión", 1, 10, 3, "intentos"),
        new(DuracionViajeMinutos, "Duración estimada de un viaje sin itinerario", 5, 720, 45, "min"),
    ];

    public static IReadOnlyList<DefinicionParametro> Definiciones => Todas;

    public static DefinicionParametro Obtener(string clave) =>
        Array.Find(Todas, d => d.Clave == clave)
        ?? throw new DomainException($"El parámetro '{clave}' no existe.");
}
