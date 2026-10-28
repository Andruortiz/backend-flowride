using System.Globalization;
using System.Text.RegularExpressions;
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

internal sealed partial class ServicioConversaciones(
    IMensajeEntranteRepository mensajes,
    IConsultaIdentidad identidad,
    IConversacionRepository conversaciones,
    IConsultaSolicitudes solicitudes,
    IConsultaFlota flota,
    IConsultaAsignaciones asignaciones,
    IConsultaViajes viajes,
    IServicioSolicitudes servicioSolicitudes,
    IServicioAsignaciones servicioAsignaciones,
    IServicioViajes servicioViajes,
    IServicioOperador operador,
    IWhatsAppGateway whatsapp,
    IContextoActorEstablecible actor,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioConversaciones
{
    public async Task<RespuestaBotResponse> ProcesarAsync(MensajeEntranteRequest request, CancellationToken ct = default)
    {
        if (await mensajes.ExisteAsync(request.ExternalMessageId, ct))
        {
            return new RespuestaBotResponse(string.Empty, null);
        }

        var usuario = await identidad.ObtenerUsuarioPorTelefonoAsync(request.Telefono, ct);
        string texto;
        Guid? solicitudId = null;

        if (usuario is not { Activo: true })
        {
            texto = "No encontramos tu número registrado. Comunícate con tu empresa o con el operador.";
        }
        else
        {
            actor.Establecer(usuario.Id, usuario.Nombre, usuario.Rol);

            try
            {
                (texto, solicitudId) = usuario.Rol switch
                {
                    RolUsuario.Pasajero => await AtenderPasajeroAsync(usuario, request.Texto, ct),
                    RolUsuario.Conductor => (await AtenderConductorAsync(usuario, request.Texto, ct), null),
                    _ => ("El canal de WhatsApp es solo para pasajeros y conductores. Usa el panel web.", null),
                };
            }
            catch (Exception ex) when (ex is DomainException or ConflictException or ForbiddenException or NotFoundException)
            {
                texto = ex.Message;
            }
        }

        await mensajes.RegistrarAsync(request.ExternalMessageId, reloj.GetUtcNow(), ct);
        await unidad.GuardarCambiosAsync(ct);
        await whatsapp.EnviarAsync(request.Telefono, texto, ct);

        return new RespuestaBotResponse(texto, solicitudId);
    }

    private async Task<(string Texto, Guid? SolicitudId)> AtenderPasajeroAsync(Usuario usuario, string texto, CancellationToken ct)
    {
        if (CalificarRegex().Match(texto.Trim()) is { Success: true } coincidencia)
        {
            return (await CalificarUltimoViajeAsync(usuario, int.Parse(coincidencia.Groups[1].Value, CultureInfo.InvariantCulture), ct), null);
        }

        var ahora = reloj.GetUtcNow();
        var conversacion = await conversaciones.ObtenerPorUsuarioAsync(usuario.Id, ct);

        if (conversacion is null)
        {
            conversacion = ConversacionBot.Iniciar(usuario.Id, ahora);
            await conversaciones.AgregarAsync(conversacion, ct);
        }

        var respuesta = conversacion.Procesar(texto, ahora, await operador.ZonaAsync(ct));

        switch (respuesta.Intencion)
        {
            case IntencionConversacion.SolicitudCompleta when respuesta.Solicitud is { } datos:
                var creada = await servicioSolicitudes.RegistrarAsync(
                    new CrearSolicitudRequest(null, datos.Origen, datos.Destino, datos.FechaHoraUtc, datos.Urgente, false, null, null, null), ct);

                return (respuesta.Texto, creada.Id);

            case IntencionConversacion.ConsultarEstado:
                return (await EstadoDeUltimaSolicitudAsync(usuario, ct), null);

            default:
                return (respuesta.Texto, null);
        }
    }

    private async Task<string> EstadoDeUltimaSolicitudAsync(Usuario usuario, CancellationToken ct)
    {
        var pasajero = await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id, ct);
        var ultima = pasajero is null ? null : await solicitudes.ObtenerUltimaDePasajeroAsync(pasajero.Id, ct);

        return ultima is null
            ? "Aún no tienes solicitudes. Escribe SOLICITAR para pedir un servicio."
            : $"Tu última solicitud está en estado {ultima.Estado}.";
    }

    private async Task<string> CalificarUltimoViajeAsync(Usuario usuario, int puntaje, CancellationToken ct)
    {
        var pasajero = await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id, ct);
        var viaje = pasajero is null ? null : await viajes.ObtenerUltimoFinalizadoDePasajeroAsync(pasajero.Id, ct);

        if (viaje is null)
        {
            return "No tienes viajes finalizados para calificar.";
        }

        await servicioViajes.CalificarAsync(viaje.Id, new CalificarViajeRequest(puntaje, null), ct);

        return "¡Gracias por calificar tu viaje!";
    }

    private async Task<string> AtenderConductorAsync(Usuario usuario, string texto, CancellationToken ct)
    {
        var conductor = await flota.ObtenerConductorPorUsuarioAsync(usuario.Id, ct) ?? throw new NotFoundException("No encontramos tu perfil de conductor.");
        var partes = texto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var comando = partes.Length == 0 ? string.Empty : partes[0].ToUpperInvariant();

        switch (comando)
        {
            case "ACEPTAR" or "RECHAZAR":
                var pendiente = await asignaciones.ObtenerPendienteDeConductorAsync(conductor.Id, ct)
                    ?? throw new NotFoundException("No tienes servicios por responder.");

                if (comando == "ACEPTAR")
                {
                    await servicioAsignaciones.AceptarAsync(pendiente.Id, ct);

                    return "Servicio aceptado. Escribe INICIO cuando comience el viaje.";
                }

                await servicioAsignaciones.RechazarAsync(pendiente.Id, ct);

                return "Servicio rechazado.";

            case "INICIO":
                await servicioViajes.IniciarAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, ct);

                return "Viaje iniciado. Escribe FIN al terminar.";

            case "FIN":
                await servicioViajes.FinalizarAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, ct);

                return "Viaje finalizado. Gracias.";

            case "INCIDENTE":
                var descripcion = partes.Length > 1 ? partes[1] : string.Empty;
                await servicioViajes.ReportarIncidenteAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, new ReportarIncidenteRequest(descripcion), ct);

                return "Incidente reportado al operador.";

            default:
                return "Comandos: ACEPTAR, RECHAZAR, INICIO, FIN, INCIDENTE <descripción>.";
        }
    }

    private async Task<FlowRide.Viajes.Dominio.Viaje> ViajeVigenteAsync(Guid conductorId, CancellationToken ct) =>
        await viajes.ObtenerVigenteDeConductorAsync(conductorId, ct) ?? throw new NotFoundException("No tienes un viaje programado o en curso.");

    [GeneratedRegex(@"^calificar\s+([1-5])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CalificarRegex();
}
