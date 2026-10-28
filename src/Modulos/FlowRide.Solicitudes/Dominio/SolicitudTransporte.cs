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

/// <summary>Pedido de transporte de un pasajero. Su estado es la fuente de verdad del avance del servicio.</summary>
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

    /// <summary>Coordenadas del destino, si se conocen. Sin ellas no se estima la ETA, solo se monitorea.</summary>
    public double? DestinoLatitud { get; }

    public double? DestinoLongitud { get; }

    /// <summary>Instante UTC del servicio.</summary>
    public DateTimeOffset FechaHoraProgramada { get; private set; }

    public bool Urgente { get; }

    /// <summary>Si es personal, administración y operación logística no ven origen ni destino (PAS-SEG-01).</summary>
    public bool EsPersonal { get; }

    public EstadoSolicitud Estado { get; private set; }

    public Guid? AsignacionId { get; private set; }

    /// <summary>Asignaciones que no prosperaron (rechazo o sin recurso).</summary>
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

    /// <summary>RF-32: solo se puede modificar mientras no tenga conductor.</summary>
    public void Modificar(string origen, string destino, DateTimeOffset fechaHoraProgramada, DateTimeOffset ahora)
    {
        if (!EstaAbierta)
        {
            throw new DomainException("Solo se puede modificar una solicitud sin conductor; cancélala y crea otra.");
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
            throw new DomainException($"La solicitud está {Estado} y no admite una asignación nueva.");
        }

        Estado = EstadoSolicitud.Asignada;
        AsignacionId = asignacionId;
    }

    /// <summary>RF-12: el operador cambia el conductor de una solicitud ya asignada sin contarlo como intento fallido.</summary>
    public void CambiarAsignacion(Guid nuevaAsignacionId)
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("Solo una solicitud asignada admite el cambio de asignación.");
        }

        AsignacionId = nuevaAsignacionId;
    }

    /// <summary>El conductor rechazó o la asignación se deshizo: vuelve a la cola.</summary>
    public void VolverAPendiente()
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("Solo una solicitud asignada puede volver a pendiente.");
        }

        Estado = EstadoSolicitud.Pendiente;
        AsignacionId = null;
        Intentos++;
    }

    public void MarcarEnRevision(string motivo, DateTimeOffset ahora)
    {
        if (Estado != EstadoSolicitud.Pendiente)
        {
            throw new DomainException("Solo una solicitud pendiente puede pasar a revisión.");
        }

        Estado = EstadoSolicitud.EnRevision;
        Intentos++;
        Registrar(new SinRecursoDisponible(Id, PasajeroId, motivo, ahora));
    }

    public void IniciarServicio()
    {
        if (Estado != EstadoSolicitud.Asignada)
        {
            throw new DomainException("Solo una solicitud asignada puede iniciar el servicio.");
        }

        Estado = EstadoSolicitud.EnCurso;
    }

    public void Finalizar()
    {
        if (Estado != EstadoSolicitud.EnCurso)
        {
            throw new DomainException("Solo un servicio en curso puede finalizar.");
        }

        Estado = EstadoSolicitud.Finalizada;
    }

    /// <summary>
    /// RF-32. El pasajero cancela dentro de la ventana; el operador puede hacerlo siempre que el servicio no haya empezado.
    /// </summary>
    public void Cancelar(string motivo, bool esOperador, int ventanaMinutos, DateTimeOffset ahora)
    {
        if (Estado is EstadoSolicitud.EnCurso or EstadoSolicitud.Finalizada or EstadoSolicitud.Cancelada)
        {
            throw new DomainException($"Una solicitud {Estado} no se puede cancelar.");
        }

        if (TextHelper.IsEmptyWithTrim(motivo))
        {
            throw new DomainException("Indica el motivo de la cancelación.");
        }

        if (!esOperador && FechaHoraProgramada - ahora < TimeSpan.FromMinutes(ventanaMinutos))
        {
            throw new DomainException(
                $"Solo puedes cancelar con al menos {ventanaMinutos} minutos de antelación; contacta al operador.");
        }

        var asignacionPrevia = AsignacionId;
        Estado = EstadoSolicitud.Cancelada;
        AsignacionId = null;
        MotivoCancelacion = motivo.Trim();
        CanceladaEn = ahora;
        Registrar(new SolicitudCancelada(Id, PasajeroId, asignacionPrevia, MotivoCancelacion, ahora));
    }

    /// <summary>Marca el recordatorio y emite el evento una sola vez (RF-35).</summary>
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
            throw new DomainException("Indica la latitud y la longitud del destino, o ninguna.");
        }

        if (double.IsNaN(latitud.Value) || latitud is < -90 or > 90 || double.IsNaN(longitud.Value) || longitud is < -180 or > 180)
        {
            throw new DomainException("Las coordenadas del destino están fuera de rango.");
        }
    }

    private static void ValidarRuta(string origen, string destino, DateTimeOffset fechaHora, DateTimeOffset ahora)
    {
        if (TextHelper.IsEmptyWithTrim(origen) || TextHelper.IsEmptyWithTrim(destino))
        {
            throw new DomainException("El origen y el destino de la solicitud son obligatorios.");
        }

        if (string.Equals(origen.Trim(), destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("El origen y el destino deben ser distintos.");
        }

        if (fechaHora <= ahora)
        {
            throw new DomainException("La fecha y hora del servicio debe ser futura.");
        }
    }
}
