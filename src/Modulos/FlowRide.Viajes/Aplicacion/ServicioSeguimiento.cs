using Microsoft.Extensions.Logging;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Observabilidad;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Nucleo.TiempoReal;
using FlowRide.Contratos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Viajes.Aplicacion;

internal sealed partial class ServicioSeguimiento(
    IUbicacionRepository ubicaciones,
    IConsultaFlota flota,
    IViajeRepository viajes,
    IServicioParametros parametros,
    IUnitOfWork unidad,
    TimeProvider reloj,
    ITransmisorTiempoReal tiempoReal,
    ILogger<ServicioSeguimiento> logger) : IServicioSeguimiento
{
    private const int PuntosParaEstimar = 5;

    public async Task<IReadOnlyList<UbicacionVehiculoResponse>> UltimasAsync(int horas, CancellationToken ct = default) =>
        [.. (await ubicaciones.ObtenerUltimasPorVehiculoAsync(reloj.GetUtcNow().AddHours(-horas), ct))
            .Select(u => new UbicacionVehiculoResponse(u.VehiculoId, u.ViajeId, u.Latitud, u.Longitud, u.TimestampOrigen, u.Fuente))];

    public async Task<ResultadoIngesta> IngerirAsync(IReadOnlyList<ReportarUbicacionRequest> eventos, CancellationToken ct = default)
    {
        int aceptadas = 0, duplicadas = 0, rechazadas = 0;

        foreach (var evento in eventos)
        {
            try
            {
                if ((await ReportarAsync(evento, ct)).Duplicada)
                {
                    duplicadas++;
                }
                else
                {
                    aceptadas++;
                }
            }
            catch (Exception e) when (e is DomainException or NotFoundException)
            {
                rechazadas++;
                EventoRechazado(logger, evento.Fuente, e.Message);
            }
        }

        Metricas.UbicacionesIngeridas.Add(aceptadas, Metricas.Etiqueta("resultado", "aceptada"));
        Metricas.UbicacionesIngeridas.Add(duplicadas, Metricas.Etiqueta("resultado", "duplicada"));
        Metricas.UbicacionesIngeridas.Add(rechazadas, Metricas.Etiqueta("resultado", "rechazada"));

        return new ResultadoIngesta(aceptadas, duplicadas, rechazadas);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Evento de telemetría de {Fuente} rechazado: {Motivo}")]
    private static partial void EventoRechazado(ILogger logger, string fuente, string motivo);

    public async Task<UbicacionResponse> ReportarAsync(ReportarUbicacionRequest request, CancellationToken ct = default)
    {
        _ = await flota.ObtenerVehiculoAsync(request.VehiculoId, ct) ?? throw new NotFoundException("error.el_vehiculo_no_existe");

        var fuente = request.Fuente.Trim().ToLowerInvariant();

        if (await ubicaciones.ExisteAsync(request.VehiculoId, request.TimestampOrigen, fuente, ct))
        {
            return new UbicacionResponse(request.VehiculoId, request.Latitud, request.Longitud, request.TimestampOrigen, true);
        }

        var ahora = reloj.GetUtcNow();
        var ventana = await parametros.ValorAsync(CatalogoParametros.VentanaEventoGpsMinutos, ct);
        var viaje = await viajes.ObtenerEnCursoDeVehiculoAsync(request.VehiculoId, ct);

        var registro = RegistroUbicacion.Crear(
            request.VehiculoId,
            viaje?.ConductorId,
            viaje?.Id,
            request.TimestampOrigen,
            ahora,
            request.Latitud,
            request.Longitud,
            request.Continua ? CalidadUbicacion.Continua : CalidadUbicacion.Puntual,
            fuente,
            ventana);

        await ubicaciones.AgregarAsync(registro, ct);

        if (viaje is { DestinoLatitud: { } lat, DestinoLongitud: { } lon })
        {
            await EvaluarEtaAsync(viaje, Coordenada.Crear(lat, lon), ahora, ct);
        }

        await unidad.GuardarCambiosAsync(ct);
        await tiempoReal.PublicarAsync(TemasTiempoReal.Ubicaciones, ct);

        return new UbicacionResponse(request.VehiculoId, request.Latitud, request.Longitud, request.TimestampOrigen, false);
    }

    private async Task EvaluarEtaAsync(Viaje viaje, Coordenada destino, DateTimeOffset ahora, CancellationToken ct)
    {
        var recientes = await ubicaciones.ObtenerRecientesAsync(viaje.VehiculoId, ahora.AddMinutes(-30), PuntosParaEstimar, ct);

        if (EstimadorEta.EstimarLlegada(recientes, destino) is not { } eta)
        {
            return;
        }

        var umbral = await parametros.ValorAsync(CatalogoParametros.UmbralEtaMinutos, ct);
        viaje.EvaluarDesviacionEta(eta, umbral, ahora);
    }
}
