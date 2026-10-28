namespace FlowRide.Viajes.Aplicacion.Puertos;

/// <summary>Cómo otros módulos piden crear o cancelar el viaje programado, sin conocer sus repositorios.</summary>
public interface IProgramacionViajes
{
    /// <summary>Crea el viaje de una asignación aceptada. Se confirma con la transacción de quien lo pide.</summary>
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

    /// <summary>Cancela el viaje si todavía no empezó. No hace nada si no existe o ya empezó.</summary>
    Task CancelarProgramadoDeAsignacionAsync(Guid asignacionId, DateTimeOffset ahora, CancellationToken ct = default);
}
