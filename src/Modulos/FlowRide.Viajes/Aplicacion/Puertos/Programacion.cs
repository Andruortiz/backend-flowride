namespace FlowRide.Viajes.Aplicacion.Puertos;

public interface IProgramacionViajes
{
    Task ProgramarAsync(
        Guid solicitudId,
        Guid asignacionId,
        Guid pasajeroId,
        Guid conductorId,
        Guid vehiculoId,
        DateTimeOffset programadoPara,
        TimeSpan duracion,
        double? destinoLatitud,
        double? destinoLongitud,
        DateTimeOffset ahora,
        CancellationToken ct = default);

    Task CancelarProgramadoDeAsignacionAsync(Guid asignacionId, DateTimeOffset ahora, CancellationToken ct = default);
}
