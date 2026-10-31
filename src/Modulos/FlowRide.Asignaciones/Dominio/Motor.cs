using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Viajes.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Asignaciones.Dominio;

public enum TipoRegla
{
    Eliminatoria = 1,
    Prioridad = 2,
    Puntuacion = 3,
}

public sealed record CandidatoAsignacion(Conductor Conductor, Vehiculo? Vehiculo, int CargaActual, int AsignacionesRecientes)
{
    public EvaluacionAsignabilidad Asignabilidad(DateOnly fecha, TimeOnly hora) =>
        Conductor.EvaluarAsignabilidad(Vehiculo, fecha, hora);
}

public sealed record ContextoAsignacion(SolicitudTransporte Solicitud, DateOnly Fecha, TimeOnly Hora, int PasajerosRequeridos = 1);

public sealed record EvaluacionRegla(bool Descarta, string? Motivo, double Puntos)
{
    public static EvaluacionRegla Pasa { get; } = new(false, null, 0);

    public static EvaluacionRegla Descartar(string motivo) => new(true, motivo, 0);

    public static EvaluacionRegla ConPuntos(double puntos) => new(false, null, puntos);
}

public interface IAssignmentRule
{
    string Id { get; }

    string Nombre { get; }

    TipoRegla Tipo { get; }

    bool Obligatoria { get; }

    EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato);
}

public sealed class ReglaDisponibilidad : IAssignmentRule
{
    public string Id => "disponibilidad";

    public string Nombre => "Disponibilidad";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato)
    {
        var motivos = candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos;

        if (motivos.Contains(MotivoNoAsignable.ConductorInactivo))
        {
            return EvaluacionRegla.Descartar("El conductor está inactivo");
        }

        return motivos.Contains(MotivoNoAsignable.ConductorNoDisponible)
            ? EvaluacionRegla.Descartar("El conductor se declaró no disponible")
            : EvaluacionRegla.Pasa;
    }
}

public sealed class ReglaCompatibilidadTurno : IAssignmentRule
{
    public string Id => "turno";

    public string Nombre => "Compatibilidad de turno";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos.Contains(MotivoNoAsignable.FueraDeTurno)
            ? EvaluacionRegla.Descartar("El servicio queda fuera de su turno")
            : EvaluacionRegla.Pasa;
}

public sealed class ReglaRestriccionRegulatoria : IAssignmentRule
{
    public string Id => "regulatoria";

    public string Nombre => "Restricción regulatoria";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato)
    {
        var motivos = candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos;

        if (motivos.Contains(MotivoNoAsignable.LicenciaNoVigente))
        {
            return EvaluacionRegla.Descartar("Licencia no vigente en la fecha del servicio");
        }

        if (motivos.Contains(MotivoNoAsignable.SinVehiculo))
        {
            return EvaluacionRegla.Descartar("No tiene vehículo asignado");
        }

        if (motivos.Contains(MotivoNoAsignable.VehiculoNoHabilitado))
        {
            return EvaluacionRegla.Descartar("Vehículo no habilitado");
        }

        return motivos.Contains(MotivoNoAsignable.DocumentosVehiculoNoVigentes)
            ? EvaluacionRegla.Descartar("Documentos del vehículo no vigentes en la fecha del servicio")
            : EvaluacionRegla.Pasa;
    }
}

public sealed class ReglaCapacidadServicio : IAssignmentRule
{
    public string Id => "capacidad";

    public string Nombre => "Capacidad del servicio";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        candidato.Vehiculo is { } vehiculo && vehiculo.Capacidad < contexto.PasajerosRequeridos
            ? EvaluacionRegla.Descartar($"El vehículo admite {vehiculo.Capacidad} y se requieren {contexto.PasajerosRequeridos}")
            : EvaluacionRegla.Pasa;
}

public sealed class ReglaServicioUrgente : IAssignmentRule
{
    public string Id => "urgente";

    public string Nombre => "Servicio urgente";

    public TipoRegla Tipo => TipoRegla.Prioridad;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        EvaluacionRegla.ConPuntos(contexto.Solicitud.Urgente && candidato.CargaActual == 0 ? 100 : 0);
}

public sealed class ReglaBalanceCarga : IAssignmentRule
{
    public string Id => "balance_carga";

    public string Nombre => "Balance de carga";

    public TipoRegla Tipo => TipoRegla.Puntuacion;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        EvaluacionRegla.ConPuntos(Math.Max(0, 100 - (candidato.CargaActual * 15)));
}

public sealed class ReglaAsignacionesRecientes : IAssignmentRule
{
    public string Id => "recientes";

    public string Nombre => "Asignaciones recientes";

    public TipoRegla Tipo => TipoRegla.Puntuacion;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        EvaluacionRegla.ConPuntos(Math.Max(0, 100 - (candidato.AsignacionesRecientes * 20)));
}

public static class CatalogoReglas
{
    public static IReadOnlyList<IAssignmentRule> Todas { get; } =
    [
        new ReglaDisponibilidad(),
        new ReglaCompatibilidadTurno(),
        new ReglaRestriccionRegulatoria(),
        new ReglaCapacidadServicio(),
        new ReglaServicioUrgente(),
        new ReglaBalanceCarga(),
        new ReglaAsignacionesRecientes(),
    ];
}
