using FlowRide.Contratos;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.TiempoReal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowRide.Inicializador.TiempoReal;

[Authorize(Politicas.Gestion)]
internal sealed class HubTiempoReal : Hub;

internal sealed class TransmisorSignalR(IHubContext<HubTiempoReal> hub) : ITransmisorTiempoReal
{
    public Task PublicarAsync(string tema, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync("cambio", tema, ct);
}

internal static class TemasPorEvento
{
    private static readonly Dictionary<Type, string[]> Mapa = new()
    {
        [typeof(SolicitudRegistrada)] = [TemasTiempoReal.Solicitudes],
        [typeof(SolicitudCancelada)] = [TemasTiempoReal.Solicitudes, TemasTiempoReal.Asignaciones],
        [typeof(SinRecursoDisponible)] = [TemasTiempoReal.Solicitudes, TemasTiempoReal.Alertas],
        [typeof(AsignacionCreada)] = [TemasTiempoReal.Asignaciones, TemasTiempoReal.Solicitudes],
        [typeof(AsignacionAceptada)] = [TemasTiempoReal.Asignaciones, TemasTiempoReal.Solicitudes],
        [typeof(AsignacionRechazada)] = [TemasTiempoReal.Asignaciones, TemasTiempoReal.Solicitudes],
        [typeof(AsignacionReasignada)] = [TemasTiempoReal.Asignaciones, TemasTiempoReal.Solicitudes],
        [typeof(ViajeIniciado)] = [TemasTiempoReal.Viajes, TemasTiempoReal.Solicitudes, TemasTiempoReal.Asignaciones],
        [typeof(ViajeFinalizado)] = [TemasTiempoReal.Viajes, TemasTiempoReal.Solicitudes, TemasTiempoReal.Asignaciones],
        [typeof(IncidenteReportado)] = [TemasTiempoReal.Viajes, TemasTiempoReal.Alertas],
        [typeof(DesviacionEtaDetectada)] = [TemasTiempoReal.Viajes, TemasTiempoReal.Alertas],
    };

    public static IReadOnlyList<string> De(Type evento) => Mapa.TryGetValue(evento, out var temas) ? temas : [];
}

internal sealed class ManejadorTiempoReal<TEvento>(ITransmisorTiempoReal transmisor) : IManejadorEvento<TEvento>
    where TEvento : IEventoDominio
{
    public async Task ManejarAsync(TEvento evento, CancellationToken ct = default)
    {
        foreach (var tema in TemasPorEvento.De(typeof(TEvento)))
        {
            await transmisor.PublicarAsync(tema, ct);
        }
    }
}

internal static class RegistroTiempoReal
{
    public const string Ruta = "/api/v1/tiempo-real";

    public static IServiceCollection AgregarTiempoReal(this IServiceCollection servicios)
    {
        servicios.AddSignalR();
        servicios.AddSingleton<ITransmisorTiempoReal, TransmisorSignalR>();
        servicios.AddScoped(typeof(IManejadorEvento<>), typeof(ManejadorTiempoReal<>));

        return servicios;
    }

    public static IEndpointRouteBuilder MapearTiempoReal(this IEndpointRouteBuilder rutas)
    {
        rutas.MapHub<HubTiempoReal>(Ruta);

        return rutas;
    }
}
