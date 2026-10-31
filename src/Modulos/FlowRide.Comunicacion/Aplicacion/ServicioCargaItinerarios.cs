using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Aplicacion;

internal sealed class ServicioCargaItinerarios(
    IConsultaIdentidad identidad,
    IConsultaClientes consultaClientes,
    IServicioClientes clientes,
    IServicioSolicitudes solicitudes,
    IServicioOperador operador,
    TimeProvider reloj) : IServicioCargaItinerarios
{
    public ResultadoLectura Previsualizar(string contenido) => LectorItinerarioCsv.Leer(contenido);

    public async Task<ResultadoCarga> ConfirmarAsync(Guid pasajeroId, IReadOnlyList<FilaItinerario> filas, CancellationToken ct = default)
    {
        var pasajero = await identidad.ObtenerPasajeroAsync(pasajeroId, ct) ?? throw new NotFoundException("El pasajero no existe.");
        var domicilio = pasajero.Domicilio ?? throw new DomainException("Registra tu domicilio antes de cargar el itinerario.");
        var zona = await operador.ZonaAsync(ct);
        var creados = new List<ViajeCargado>();
        var omitidos = new List<ErrorFila>();

        foreach (var fila in filas)
        {
            var recogida = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(fila.Fecha.ToDateTime(fila.HoraSalida), zona), TimeSpan.Zero);

            if (recogida <= reloj.GetUtcNow())
            {
                omitidos.Add(new ErrorFila(fila.Linea, "La hora de recogida ya pasó."));

                continue;
            }

            if (string.Equals(domicilio, fila.Destino, StringComparison.OrdinalIgnoreCase))
            {
                omitidos.Add(new ErrorFila(fila.Linea, "El destino no puede ser igual a tu domicilio."));

                continue;
            }

            if (await consultaClientes.ExisteItinerarioDePasajeroAsync(pasajero.Id, fila.CodigoServicio, fila.Fecha, ct))
            {
                omitidos.Add(new ErrorFila(fila.Linea, "Este viaje ya estaba cargado."));

                continue;
            }

            var itinerario = await clientes.CrearItinerarioAsync(
                new CrearItinerarioRequest(fila.CodigoServicio, pasajero.ClienteEmpresarialId, domicilio, fila.Destino, fila.Fecha, fila.HoraSalida, fila.HoraLlegada, pasajero.Id), ct);
            var solicitud = await solicitudes.RegistrarAsync(
                new CrearSolicitudRequest(pasajero.Id, domicilio, fila.Destino, recogida, false, false, itinerario.Id, null, null), ct);

            creados.Add(new ViajeCargado(fila.Linea, fila.CodigoServicio, fila.Fecha, solicitud.Id));
        }

        return new ResultadoCarga(creados, omitidos);
    }
}
