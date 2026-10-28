using FlowRide.Nucleo.Dominio;

namespace FlowRide.Contratos;

public sealed record AsignacionCreada(Guid AsignacionId, Guid SolicitudId, Guid ConductorId, Guid VehiculoId, bool EsManual, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record AsignacionAceptada(Guid AsignacionId, Guid SolicitudId, Guid ConductorId, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record AsignacionRechazada(Guid AsignacionId, Guid SolicitudId, Guid ConductorId, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record AsignacionReasignada(
    Guid AsignacionAnteriorId,
    Guid AsignacionNuevaId,
    Guid SolicitudId,
    Guid ConductorAnteriorId,
    Guid ConductorNuevoId,
    string Motivo,
    DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record SolicitudRegistrada(Guid SolicitudId, Guid PasajeroId, bool Urgente, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record SolicitudCancelada(Guid SolicitudId, Guid PasajeroId, Guid? AsignacionId, string Motivo, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record SinRecursoDisponible(Guid SolicitudId, Guid PasajeroId, string Motivo, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record RecordatorioViajeDebido(Guid SolicitudId, Guid PasajeroId, Guid? AsignacionId, DateTimeOffset FechaHoraProgramada, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record ViajeIniciado(Guid ViajeId, Guid SolicitudId, Guid PasajeroId, Guid ConductorId, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record ViajeFinalizado(Guid ViajeId, Guid SolicitudId, Guid PasajeroId, Guid ConductorId, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record IncidenteReportado(Guid ViajeId, Guid ConductorId, string Descripcion, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record DesviacionEtaDetectada(
    Guid ViajeId,
    Guid PasajeroId,
    double DesvioMinutos,
    int UmbralMinutos,
    DateTimeOffset EtaEstimada,
    DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);

public sealed record UsuarioDesactivado(Guid UsuarioId, DateTimeOffset OcurrioEn)
    : EventoDominio(OcurrioEn);
