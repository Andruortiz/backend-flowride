using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Viajes.Dominio;

public enum EstadoViaje
{
    Programado = 1,
    EnCurso = 2,
    Finalizado = 3,
    Cancelado = 4,
}

/// <summary>Entrada de la trazabilidad del viaje (RF-40).</summary>
public sealed record HitoViaje(DateTimeOffset Instante, string Tipo, string Detalle);

/// <summary>Ejecución real de un servicio asignado: inicio, fin, incidentes y desviación de ETA.</summary>
public sealed class Viaje : RaizAgregado
{
    private readonly List<HitoViaje> _hitos = [];

    private Viaje(
        Guid id,
        Guid solicitudId,
        Guid asignacionId,
        Guid pasajeroId,
        Guid conductorId,
        Guid vehiculoId,
        DateTimeOffset programadoPara,
        DateTimeOffset etaProgramada,
        double? destinoLatitud,
        double? destinoLongitud)
        : base(id)
    {
        DestinoLatitud = destinoLatitud;
        DestinoLongitud = destinoLongitud;
        SolicitudId = solicitudId;
        AsignacionId = asignacionId;
        PasajeroId = pasajeroId;
        ConductorId = conductorId;
        VehiculoId = vehiculoId;
        ProgramadoPara = programadoPara;
        EtaProgramada = etaProgramada;
        Estado = EstadoViaje.Programado;
    }

    public Guid SolicitudId { get; }

    public Guid AsignacionId { get; }

    public Guid PasajeroId { get; }

    public Guid ConductorId { get; }

    public Guid VehiculoId { get; }

    public EstadoViaje Estado { get; private set; }

    public DateTimeOffset ProgramadoPara { get; }

    /// <summary>Llegada prevista (UTC) contra la que se mide la desviación.</summary>
    public DateTimeOffset EtaProgramada { get; }

    public double? DestinoLatitud { get; }

    public double? DestinoLongitud { get; }

    public DateTimeOffset? IniciadoEn { get; private set; }

    public DateTimeOffset? FinalizadoEn { get; private set; }

    /// <summary>Hay una alerta de desviación abierta; evita repetirla en cada evento GPS.</summary>
    public bool AlertaEtaActiva { get; private set; }

    public IReadOnlyList<HitoViaje> Hitos => _hitos;

    public static Viaje Crear(
        Guid solicitudId,
        Guid asignacionId,
        Guid pasajeroId,
        Guid conductorId,
        Guid vehiculoId,
        DateTimeOffset programadoPara,
        DateTimeOffset etaProgramada,
        DateTimeOffset ahora,
        Guid? id = null,
        double? destinoLatitud = null,
        double? destinoLongitud = null)
    {
        if (etaProgramada <= programadoPara)
        {
            throw new DomainException("La llegada prevista debe ser posterior a la salida programada.");
        }

        var viaje = new Viaje(
            UuidHelper.GetOrGenerate(id),
            solicitudId,
            asignacionId,
            pasajeroId,
            conductorId,
            vehiculoId,
            programadoPara,
            etaProgramada,
            destinoLatitud,
            destinoLongitud);

        viaje._hitos.Add(new HitoViaje(ahora, "Programado", "Viaje programado tras la aceptación del conductor"));

        return viaje;
    }

    public void Iniciar(DateTimeOffset ahora)
    {
        if (Estado != EstadoViaje.Programado)
        {
            throw new DomainException($"Un viaje {Estado} no se puede iniciar.");
        }

        Estado = EstadoViaje.EnCurso;
        IniciadoEn = ahora;
        _hitos.Add(new HitoViaje(ahora, "Inicio", "El conductor reportó el inicio del viaje"));
        Registrar(new ViajeIniciado(Id, SolicitudId, PasajeroId, ConductorId, ahora));
    }

    public void Finalizar(DateTimeOffset ahora)
    {
        if (Estado != EstadoViaje.EnCurso)
        {
            throw new DomainException($"Un viaje {Estado} no se puede finalizar.");
        }

        Estado = EstadoViaje.Finalizado;
        FinalizadoEn = ahora;
        AlertaEtaActiva = false;
        _hitos.Add(new HitoViaje(ahora, "Fin", "El conductor reportó la finalización del viaje"));
        Registrar(new ViajeFinalizado(Id, SolicitudId, PasajeroId, ConductorId, ahora));
    }

    public void ReportarIncidente(string descripcion, DateTimeOffset ahora)
    {
        if (Estado != EstadoViaje.EnCurso)
        {
            throw new DomainException("Solo se reporta un incidente en un viaje en curso.");
        }

        if (TextHelper.IsEmptyWithTrim(descripcion))
        {
            throw new DomainException("Describe el incidente.");
        }

        var detalle = descripcion.Trim();
        _hitos.Add(new HitoViaje(ahora, "Incidente", detalle));
        Registrar(new IncidenteReportado(Id, ConductorId, detalle, ahora));
    }

    public void Cancelar(DateTimeOffset ahora)
    {
        if (Estado != EstadoViaje.Programado)
        {
            throw new DomainException($"Un viaje {Estado} no se puede cancelar.");
        }

        Estado = EstadoViaje.Cancelado;
        _hitos.Add(new HitoViaje(ahora, "Cancelado", "El viaje se canceló antes de iniciar"));
    }

    /// <summary>
    /// RF-56. Emite el evento solo al cruzar el umbral y rearma la alerta cuando la ETA vuelve a ser normal.
    /// </summary>
    public void EvaluarDesviacionEta(DateTimeOffset etaEstimada, int umbralMinutos, DateTimeOffset ahora)
    {
        if (Estado != EstadoViaje.EnCurso)
        {
            return;
        }

        var desvio = (etaEstimada - EtaProgramada).TotalMinutes;

        if (desvio > umbralMinutos)
        {
            if (AlertaEtaActiva)
            {
                return;
            }

            AlertaEtaActiva = true;
            _hitos.Add(new HitoViaje(ahora, "DesviacionEta", $"ETA +{desvio:0.#} min sobre lo programado"));
            Registrar(new DesviacionEtaDetectada(Id, PasajeroId, Math.Round(desvio, 1), umbralMinutos, etaEstimada, ahora));
        }
        else
        {
            AlertaEtaActiva = false;
        }
    }
}
