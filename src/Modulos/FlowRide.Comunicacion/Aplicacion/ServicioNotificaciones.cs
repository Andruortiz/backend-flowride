using Microsoft.Extensions.Logging;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Comunicacion.Aplicacion;

internal sealed partial class ServicioNotificaciones(
    IConsultaIdentidad identidad,
    IConsultaFlota flota,
    IWhatsAppGateway whatsapp,
    INotificacionEnviadaRepository enviadas,
    IUnitOfWork unidad,
    TimeProvider reloj,
    ILogger<ServicioNotificaciones> logger) : IServicioNotificaciones
{
    public async Task EnviarAPasajeroAsync(Guid pasajeroId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default)
    {
        var pasajero = await identidad.ObtenerPasajeroAsync(pasajeroId, ct) ?? throw new NotFoundException("El pasajero no existe.");
        await EnviarAUsuarioAsync(pasajero.UsuarioId, eventoId, tipo, claveMensaje, variables, ct);
    }

    public async Task EnviarAConductorAsync(Guid conductorId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default)
    {
        var conductor = await flota.ObtenerConductorAsync(conductorId, ct) ?? throw new NotFoundException("El conductor no existe.");
        await EnviarAUsuarioAsync(conductor.UsuarioId, eventoId, tipo, claveMensaje, variables, ct);
    }

    public async Task EnviarAUsuarioAsync(Guid usuarioId, Guid eventoId, TipoNotificacion tipo, string claveMensaje, IReadOnlyDictionary<string, string> variables, CancellationToken ct = default)
    {
        var usuario = await identidad.ObtenerUsuarioAsync(usuarioId, ct) ?? throw new NotFoundException("El destinatario no existe.");
        var solicitud = new SolicitudNotificacion(eventoId, usuarioId, usuario.Rol, tipo, claveMensaje, variables);

        foreach (var canal in PoliticaCanal.Resolver(usuario.Rol))
        {
            if (canal != CanalNotificacion.WhatsApp)
            {
                continue;
            }

            if (usuario.Telefono is null || !usuario.Activo)
            {
                OmitidaSinDestino(logger, tipo);
                continue;
            }

            if (await enviadas.ExisteAsync(solicitud.EventoId, usuarioId, canal, ct))
            {
                continue;
            }

            await whatsapp.EnviarAsync(usuario.Telefono, CatalogoMensajes.Renderizar(solicitud.ClaveMensaje, solicitud.Variables), ct);
            await enviadas.RegistrarAsync(solicitud.EventoId, usuarioId, canal, solicitud.ClaveMensaje, reloj.GetUtcNow(), ct);
            await unidad.GuardarCambiosAsync(ct);
        }
    }
}

internal sealed partial class ServicioNotificaciones
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Notificación {Tipo} omitida: el destinatario no tiene teléfono o está inactivo.")]
    private static partial void OmitidaSinDestino(ILogger logger, TipoNotificacion tipo);
}

internal sealed class ServicioAlertas(IAlertaRepository alertas, IUnitOfWork unidad, TimeProvider reloj) : IServicioAlertas
{
    public async Task CrearAsync(string tipo, SeveridadAlerta severidad, string titulo, string detalle, string referencia, string claveDeduplicacion, CancellationToken ct = default)
    {
        if (await alertas.ExisteClaveAsync(claveDeduplicacion, ct))
        {
            return;
        }

        await alertas.AgregarAsync(AlertaOperativa.Crear(tipo, severidad, titulo, detalle, referencia, claveDeduplicacion, reloj.GetUtcNow()), ct);
        await unidad.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<AlertaResponse>> ListarAsync(bool soloNoLeidas, int pagina, int tamano, CancellationToken ct = default) =>
        [.. (await alertas.ListarAsync(soloNoLeidas, pagina, tamano, ct)).Select(a => a.A())];

    public Task<int> ContarNoLeidasAsync(CancellationToken ct = default) => alertas.ContarNoLeidasAsync(ct);

    public async Task MarcarLeidaAsync(Guid id, CancellationToken ct = default)
    {
        var alerta = await alertas.ObtenerAsync(id, ct) ?? throw new NotFoundException("La alerta no existe.");
        alerta.MarcarLeida(reloj.GetUtcNow());
        await unidad.GuardarCambiosAsync(ct);
    }
}
