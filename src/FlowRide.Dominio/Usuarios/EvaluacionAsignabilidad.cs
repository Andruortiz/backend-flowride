namespace FlowRide.Dominio.Usuarios;

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
