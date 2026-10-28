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

namespace FlowRide.Flota.Dominio;

public enum MotivoNoAsignable
{
    ConductorInactivo = 1,
    ConductorNoDisponible = 2,
    LicenciaNoVigente = 3,
    SinVehiculo = 4,
    VehiculoNoHabilitado = 5,
    DocumentosVehiculoNoVigentes = 6,
    FueraDeTurno = 7,
}

public sealed record EvaluacionAsignabilidad(IReadOnlyList<MotivoNoAsignable> Motivos)
{
    public bool EsAsignable => Motivos.Count == 0;
}
