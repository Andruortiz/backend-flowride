using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;

namespace FlowRide.Viajes.Aplicacion;

internal sealed class ProgramacionViajes(IViajeRepository viajes) : IProgramacionViajes
{
    public async Task ProgramarAsync(
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
        CancellationToken ct = default)
    {
        var viaje = Viaje.Crear(
            solicitudId, asignacionId, pasajeroId, conductorId, vehiculoId, programadoPara, programadoPara + duracion, ahora,
            destinoLatitud: destinoLatitud, destinoLongitud: destinoLongitud);

        await viajes.AgregarAsync(viaje, ct);
    }

    public async Task CancelarProgramadoDeAsignacionAsync(Guid asignacionId, DateTimeOffset ahora, CancellationToken ct = default)
    {
        if (await viajes.ObtenerPorAsignacionAsync(asignacionId, ct) is { Estado: EstadoViaje.Programado } viaje)
        {
            viaje.Cancelar(ahora);
        }
    }
}
