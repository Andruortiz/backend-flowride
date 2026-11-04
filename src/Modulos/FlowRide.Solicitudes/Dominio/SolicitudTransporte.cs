using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Solicitudes.Dominio;

public sealed class SolicitudTransporte : RaizAgregado
{
    private SolicitudTransporte(
        Guid id,
        Guid pasajeroId,
        Guid? itinerarioId,
        string origen,
        string destino,
        DateTimeOffset fechaHoraProgramada,
        bool urgente,
        bool esPersonal,
        DateTimeOffset creadaEn,
        double? destinoLatitud,
        double? destinoLongitud)
        : base(id)
    {
        DestinoLatitud = destinoLatitud;
        DestinoLongitud = destinoLongitud;
        PasajeroId = pasajeroId;
        ItinerarioId = itinerarioId;
        Origen = origen;
        Destino = destino;
        FechaHoraProgramada = fechaHoraProgramada;
        Urgente = urgente;
        EsPersonal = esPersonal;
        CreadaEn = creadaEn;
        Estado = EstadoSolicitud.Pendiente;
    }

    public Guid PasajeroId { get; }

    public Guid? ItinerarioId { get; }

    public string Origen { get; private set; }

    public string Destino { get; private set; }

    public double? DestinoLatitud { get; }

    public double? DestinoLongitud { get; }

    public DateTimeOffset FechaHoraProgramada { get; private set; }

    public bool Urgente { get; }

    public bool EsPersonal { get; }

    public EstadoSolicitud Estado { get; private set; }

    public Guid? AsignacionId { get; private set; }

    public int Intentos { get; private set; }

    public bool RecordatorioEnviado { get; private set; }

    public string? MotivoCancelacion { get; private set; }

    public DateTimeOffset CreadaEn { get; }

    public DateTimeOffset? CanceladaEn { get; private set; }

    public static SolicitudTransporte Registrar(
        Pasajero pasajero,
        string origen,
        string destino,
        DateTimeOffset fechaHoraProgramada,
        bool urgente,
        bool esPersonal,
        DateTimeOffset ahora,
        Guid? itinerarioId = null,
        Guid? id = null,
        double? destinoLatitud = null,
        double? destinoLongitud = null)
    {
        ArgumentNullException.ThrowIfNull(pasajero);
        ValidarRuta(origen, destino, fechaHoraProgramada, ahora);
        ValidarCoordenadas(destinoLatitud, destinoLongitud);

        var solicitud = new SolicitudTransporte(
            UuidHelper.GetOrGenerate(id),
            pasajero.Id,
            itinerarioId,
            origen.Trim(),
            destino.Trim(),
            fechaHoraProgramada,
            urgente,
            esPersonal,
            ahora,
            destinoLatitud,
            destinoLongitud);

        solicitud.Registrar(new SolicitudRegistrada(solicitud.Id, pasajero.Id, urgente, ahora));

        return solicitud;
    }

    public bool EstaAbierta => Estado is EstadoSolicitud.Pendiente or EstadoSolicitud.EnRevision;

    public void Modificar(string origen, string destino, DateTimeOffset fechaHoraProgramada, DateTimeOffset ahora)
    {
        if (!EstaAbierta)
        {
            throw new DomainException("error.solo_se_puede_modificar_una_solicitud_sin");
        }

        ValidarRuta(origen, destino, fechaHoraProgramada, ahora);

        Origen = origen.Trim();
        Destino = destino.Trim();
        FechaHoraProgramada = fechaHoraProgramada;
        RecordatorioEnviado = false;
    }

    public void MarcarAsignada(Guid asignacionId)
    {
        if (!EstaAbierta)
        {
            throw new DomainException("error.la_solicitud_esta_y_no_admite_una", ("estado", Estado));
        }

        Estado = EstadoSolicitud.Asignada;
        AsignacionId = asignacionId;
    }

    public void CambiarAsignacion(Guid nuevaAsignacionId)
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("error.solo_una_solicitud_asignada_admite_el_cambio");
        }

        AsignacionId = nuevaAsignacionId;
    }

    public void VolverAPendiente()
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("error.solo_una_solicitud_asignada_puede_volver_a");
        }

        Estado = EstadoSolicitud.Pendiente;
        AsignacionId = null;
        Intentos++;
    }

    public void MarcarEnRevision(string motivo, DateTimeOffset ahora)
    {
        if (Estado != EstadoSolicitud.Pendiente)
        {
            throw new DomainException("error.solo_una_solicitud_pendiente_puede_pasar_a");
        }

        Estado = EstadoSolicitud.EnRevision;
        Intentos++;
        Registrar(new SinRecursoDisponible(Id, PasajeroId, motivo, ahora));
    }

    public void IniciarServicio()
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("error.solo_una_solicitud_asignada_puede_iniciar_el");
        }

        Estado = EstadoSolicitud.EnCurso;
    }

    public void Finalizar()
    {
        if (Estado != EstadoSolicitud.EnCurso)
        {
            throw new DomainException("error.solo_un_servicio_en_curso_puede_finalizar");
        }

        Estado = EstadoSolicitud.Finalizada;
    }

    public void Cancelar(string motivo, bool esOperador, int ventanaMinutos, DateTimeOffset ahora)
    {
        if (Estado is EstadoSolicitud.EnCurso or EstadoSolicitud.Finalizada or EstadoSolicitud.Cancelada)
        {
            throw new DomainException("error.una_solicitud_no_se_puede_cancelar", ("estado", Estado));
        }

        if (TextHelper.IsEmptyWithTrim(motivo))
        {
            throw new DomainException("error.indica_el_motivo_de_la_cancelacion");
        }

        if (!esOperador && FechaHoraProgramada - ahora < TimeSpan.FromMinutes(ventanaMinutos))
        {
            throw new DomainException("error.cancelacion_fuera_de_ventana", ("ventanaMinutos", ventanaMinutos));
        }

        var asignacionPrevia = AsignacionId;
        Estado = EstadoSolicitud.Cancelada;
        AsignacionId = null;
        MotivoCancelacion = motivo.Trim();
        CanceladaEn = ahora;
        Registrar(new SolicitudCancelada(Id, PasajeroId, asignacionPrevia, MotivoCancelacion, ahora));
    }

    public void RegistrarRecordatorioDebido(DateTimeOffset ahora)
    {
        if (RecordatorioEnviado || Estado != EstadoSolicitud.Asignada)
        {
            return;
        }

        RecordatorioEnviado = true;
        Registrar(new RecordatorioViajeDebido(Id, PasajeroId, AsignacionId, FechaHoraProgramada, ahora));
    }

    private static void ValidarCoordenadas(double? latitud, double? longitud)
    {
        if (latitud is null && longitud is null)
        {
            return;
        }

        if (latitud is null || longitud is null)
        {
            throw new DomainException("error.indica_la_latitud_y_la_longitud_del");
        }

        if (double.IsNaN(latitud.Value) || latitud is < -90 or > 90 || double.IsNaN(longitud.Value) || longitud is < -180 or > 180)
        {
            throw new DomainException("error.las_coordenadas_del_destino_estan_fuera_de");
        }
    }

    private static void ValidarRuta(string origen, string destino, DateTimeOffset fechaHora, DateTimeOffset ahora)
    {
        if (TextHelper.IsEmptyWithTrim(origen) || TextHelper.IsEmptyWithTrim(destino))
        {
            throw new DomainException("error.el_origen_y_el_destino_de_la");
        }

        if (string.Equals(origen.Trim(), destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("error.el_origen_y_el_destino_deben_ser");
        }

        if (fechaHora <= ahora)
        {
            throw new DomainException("error.la_fecha_y_hora_del_servicio_debe");
        }
    }
}
