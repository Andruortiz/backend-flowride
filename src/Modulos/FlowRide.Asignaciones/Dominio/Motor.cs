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

public sealed record EvaluacionRegla(bool Descarta, MensajeClave? Motivo, double Puntos)
{
    public static EvaluacionRegla Pasa { get; } = new(false, null, 0);

    public static EvaluacionRegla Descartar(MensajeClave motivo) => new(true, motivo, 0);

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

    public string Nombre => "regla.disponibilidad";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato)
    {
        var motivos = candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos;

        if (motivos.Contains(MotivoNoAsignable.ConductorInactivo))
        {
            return EvaluacionRegla.Descartar(new MensajeClave("motor.el_conductor_esta_inactivo"));
        }

        return motivos.Contains(MotivoNoAsignable.ConductorNoDisponible)
            ? EvaluacionRegla.Descartar(new MensajeClave("motor.el_conductor_se_declaro_no_disponible"))
            : EvaluacionRegla.Pasa;
    }
}

public sealed class ReglaCompatibilidadTurno : IAssignmentRule
{
    public string Id => "turno";

    public string Nombre => "regla.turno";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos.Contains(MotivoNoAsignable.FueraDeTurno)
            ? EvaluacionRegla.Descartar(new MensajeClave("motor.el_servicio_queda_fuera_de_su"))
            : EvaluacionRegla.Pasa;
}

public sealed class ReglaRestriccionRegulatoria : IAssignmentRule
{
    public string Id => "regulatoria";

    public string Nombre => "regla.regulatoria";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => true;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato)
    {
        var motivos = candidato.Asignabilidad(contexto.Fecha, contexto.Hora).Motivos;

        if (motivos.Contains(MotivoNoAsignable.LicenciaNoVigente))
        {
            return EvaluacionRegla.Descartar(new MensajeClave("motor.licencia_no_vigente_en_la_fecha"));
        }

        if (motivos.Contains(MotivoNoAsignable.SinVehiculo))
        {
            return EvaluacionRegla.Descartar(new MensajeClave("motor.no_tiene_vehiculo_asignado"));
        }

        if (motivos.Contains(MotivoNoAsignable.VehiculoNoHabilitado))
        {
            return EvaluacionRegla.Descartar(new MensajeClave("motor.vehiculo_no_habilitado"));
        }

        return motivos.Contains(MotivoNoAsignable.DocumentosVehiculoNoVigentes)
            ? EvaluacionRegla.Descartar(new MensajeClave("motor.documentos_del_vehiculo_no_vigentes_en"))
            : EvaluacionRegla.Pasa;
    }
}

public sealed class ReglaCapacidadServicio : IAssignmentRule
{
    public string Id => "capacidad";

    public string Nombre => "regla.capacidad";

    public TipoRegla Tipo => TipoRegla.Eliminatoria;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        candidato.Vehiculo is { } vehiculo && vehiculo.Capacidad < contexto.PasajerosRequeridos
            ? EvaluacionRegla.Descartar(new MensajeClave("motor.capacidad_insuficiente", ("capacidad", vehiculo.Capacidad), ("requeridos", contexto.PasajerosRequeridos)))
            : EvaluacionRegla.Pasa;
}

public sealed class ReglaServicioUrgente : IAssignmentRule
{
    public string Id => "urgente";

    public string Nombre => "regla.urgente";

    public TipoRegla Tipo => TipoRegla.Prioridad;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        EvaluacionRegla.ConPuntos(contexto.Solicitud.Urgente && candidato.CargaActual == 0 ? 100 : 0);
}

public sealed class ReglaBalanceCarga : IAssignmentRule
{
    public string Id => "balance_carga";

    public string Nombre => "regla.balance_carga";

    public TipoRegla Tipo => TipoRegla.Puntuacion;

    public bool Obligatoria => false;

    public EvaluacionRegla Evaluar(ContextoAsignacion contexto, CandidatoAsignacion candidato) =>
        EvaluacionRegla.ConPuntos(Math.Max(0, 100 - (candidato.CargaActual * 15)));
}

public sealed class ReglaAsignacionesRecientes : IAssignmentRule
{
    public string Id => "recientes";

    public string Nombre => "regla.recientes";

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
