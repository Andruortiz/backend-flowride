using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FlowRide.Clientes.Aplicacion;
using FlowRide.Mensajes;
using FlowRide.Nucleo.Almacenamiento;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Observabilidad;
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
    ICatalogoMensajes catalogo,
    IServicioCargaItinerarios cargaItinerarios,
    IObjectStorage almacenamiento,
    IWhatsAppGateway whatsapp,
    IContextoActorEstablecible actor,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioConversaciones
{
    private const int MaximoArchivoBytes = 1_048_576;

    public async Task<RespuestaBotResponse> ProcesarAsync(MensajeEntranteRequest request, CancellationToken ct = default)
    {
        if (await mensajes.ExisteAsync(request.ExternalMessageId, ct))
        {
            Metricas.MensajesEntrantes.Add(1, Metricas.Etiqueta("resultado", "duplicado"));
            return new RespuestaBotResponse(string.Empty, null);
        }

        var usuario = await identidad.ObtenerUsuarioPorTelefonoAsync(request.Telefono, ct);
        string texto;
        Guid? solicitudId = null;

        Metricas.MensajesEntrantes.Add(1, Metricas.Etiqueta("resultado", usuario is { Activo: true } ? usuario.Rol.ToString() : "no_registrado"));

        if (usuario is not { Activo: true })
        {
            texto = catalogo.Texto("bot.usuario_no_registrado");
        }
        else
        {
            actor.Establecer(usuario.Id, usuario.Nombre, usuario.Rol);

            try
            {
                (texto, solicitudId) = usuario.Rol switch
                {
                    RolUsuario.Pasajero => await AtenderPasajeroAsync(usuario, request, ct),
                    RolUsuario.Conductor => (await AtenderConductorAsync(usuario, request.Texto, ct), null),
                    _ => (catalogo.Texto("bot.canal_restringido"), null),
                };
            }
            catch (NegocioException ex)
            {
                texto = catalogo.Texto(ex);
            }
        }

        await mensajes.RegistrarAsync(request.ExternalMessageId, reloj.GetUtcNow(), ct);
        await unidad.GuardarCambiosAsync(ct);
        await whatsapp.EnviarAsync(request.Telefono, texto, ct);

        return new RespuestaBotResponse(texto, solicitudId);
    }

    private async Task<(string Texto, Guid? SolicitudId)> AtenderPasajeroAsync(Usuario usuario, MensajeEntranteRequest request, CancellationToken ct)
    {
        var texto = request.Texto;

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

        if (await AtenderCargaAsync(usuario, conversacion, request, ahora, ct) is { } textoCarga)
        {
            return (textoCarga, null);
        }

        var respuesta = conversacion.Procesar(texto, ahora, await operador.ZonaAsync(ct));

        switch (respuesta.Intencion)
        {
            case IntencionConversacion.SolicitudCompleta when respuesta.Solicitud is { } datos:
                var creada = await servicioSolicitudes.RegistrarAsync(
                    new CrearSolicitudRequest(null, datos.Origen, datos.Destino, datos.FechaHoraUtc, datos.Urgente, false, null, null, null), ct);

                return (catalogo.Texto(respuesta.Clave, respuesta.Variables), creada.Id);

            case IntencionConversacion.ConsultarEstado:
                return (await EstadoDeUltimaSolicitudAsync(usuario, ct), null);

            default:
                return (catalogo.Texto(respuesta.Clave, respuesta.Variables), null);
        }
    }

    private async Task<string?> AtenderCargaAsync(Usuario usuario, ConversacionBot conversacion, MensajeEntranteRequest request, DateTimeOffset ahora, CancellationToken ct)
    {
        var comando = request.Texto.Trim().ToLowerInvariant();

        if (ahora > conversacion.ExpiraEn && conversacion.EnCarga)
        {
            conversacion.Terminar(ahora);
        }

        if (!conversacion.EnCarga)
        {
            if (comando is not ("itinerario" or "cargar"))
            {
                return request.Documento is null ? null : catalogo.Texto("carga.escribe_itinerario");
            }

            var pasajeroInicio = await PasajeroAsync(usuario, ct);

            if (pasajeroInicio.Domicilio is null)
            {
                conversacion.EsperarDomicilio(ahora);

                return catalogo.Texto("carga.pedir_domicilio");
            }

            conversacion.EsperarArchivo(ahora);

            return catalogo.Texto("carga.instruccion");
        }

        if (comando == "cancelar")
        {
            conversacion.Terminar(ahora);

            return catalogo.Texto("carga.cancelada");
        }

        var pasajero = await PasajeroAsync(usuario, ct);

        switch (conversacion.Estado)
        {
            case EstadoConversacion.EsperandoDomicilio:
                pasajero.DefinirDomicilio(request.Texto);
                conversacion.EsperarArchivo(ahora);

                return catalogo.Texto("carga.domicilio_guardado", new Dictionary<string, string> { ["instruccion"] = catalogo.Texto("carga.instruccion") });

            case EstadoConversacion.EsperandoArchivo:
                return await RecibirArchivoAsync(pasajero, conversacion, request.Documento, ahora, ct);

            default:
                return await ConfirmarCargaAsync(pasajero, conversacion, comando, ahora, ct);
        }
    }

    private async Task<string> RecibirArchivoAsync(Pasajero pasajero, ConversacionBot conversacion, DocumentoEntrante? documento, DateTimeOffset ahora, CancellationToken ct)
    {
        if (documento is null)
        {
            return catalogo.Texto("carga.instruccion");
        }

        if (documento.Contenido.Length > MaximoArchivoBytes)
        {
            return catalogo.Texto("carga.archivo_grande");
        }

        var lectura = cargaItinerarios.Previsualizar(Encoding.UTF8.GetString(documento.Contenido));

        if (lectura.Errores.Count > 0)
        {
            return catalogo.Texto("carga.errores", new Dictionary<string, string> { ["detalle"] = DetalleDeFilas(lectura.Errores) });
        }

        if (lectura.Filas.Count == 0)
        {
            return catalogo.Texto("carga.vacio");
        }

        var ruta = $"itinerarios/{pasajero.Id}/{ahora:yyyy-MM}/{Guid.CreateVersion7()}.csv";
        using (var contenido = new MemoryStream(documento.Contenido, writable: false))
        {
            await almacenamiento.GuardarAsync(ruta, contenido, "text/csv", ct);
        }

        conversacion.EsperarConfirmacionCarga(ruta, ahora);
        var desde = lectura.Filas.Min(f => f.Fecha).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var hasta = lectura.Filas.Max(f => f.Fecha).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return catalogo.Texto(
            "carga.resumen",
            new Dictionary<string, string>
            {
                ["cantidad"] = lectura.Filas.Count.ToString(CultureInfo.InvariantCulture),
                ["desde"] = desde,
                ["hasta"] = hasta,
                ["domicilio"] = pasajero.Domicilio!,
            });
    }

    private string DetalleDeFilas(IEnumerable<ErrorFila> errores) =>
        string.Join(
            "\n",
            errores.Take(5).Select(e => catalogo.Texto(
                "carga.linea",
                new Dictionary<string, string> { ["linea"] = e.Linea.ToString(CultureInfo.InvariantCulture), ["mensaje"] = e.Mensaje })));

    private async Task<string> ConfirmarCargaAsync(Pasajero pasajero, ConversacionBot conversacion, string comando, DateTimeOffset ahora, CancellationToken ct)
    {
        if (comando is not ("confirmar" or "si" or "sí"))
        {
            return catalogo.Texto("carga.confirmar_invalido");
        }

        await using var archivo = await almacenamiento.LeerAsync(conversacion.ArchivoItinerario!, ct)
            ?? throw new NotFoundException("carga.archivo_perdido");
        using var lector = new StreamReader(archivo, Encoding.UTF8);
        var lectura = cargaItinerarios.Previsualizar(await lector.ReadToEndAsync(ct));
        var resultado = await cargaItinerarios.ConfirmarAsync(pasajero.Id, lectura.Filas, ct);
        conversacion.Terminar(ahora);

        var variables = new Dictionary<string, string> { ["cantidad"] = resultado.Creados.Count.ToString(CultureInfo.InvariantCulture) };

        if (resultado.Omitidos.Count == 0)
        {
            return catalogo.Texto("carga.listo", variables);
        }

        variables["omitidos"] = resultado.Omitidos.Count.ToString(CultureInfo.InvariantCulture);
        variables["detalle"] = DetalleDeFilas(resultado.Omitidos);

        return catalogo.Texto("carga.listo_con_omitidos", variables);
    }

    private async Task<Pasajero> PasajeroAsync(Usuario usuario, CancellationToken ct) =>
        await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id, ct) ?? throw new NotFoundException("bot.pasajero.sin_perfil");

    private async Task<string> EstadoDeUltimaSolicitudAsync(Usuario usuario, CancellationToken ct)
    {
        var pasajero = await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id, ct);
        var ultima = pasajero is null ? null : await solicitudes.ObtenerUltimaDePasajeroAsync(pasajero.Id, ct);

        return ultima is null
            ? catalogo.Texto("bot.estado.sin_solicitudes")
            : catalogo.Texto("bot.estado.ultima", new Dictionary<string, string> { ["estado"] = ultima.Estado.ToString() });
    }

    private async Task<string> CalificarUltimoViajeAsync(Usuario usuario, int puntaje, CancellationToken ct)
    {
        var pasajero = await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id, ct);
        var viaje = pasajero is null ? null : await viajes.ObtenerUltimoFinalizadoDePasajeroAsync(pasajero.Id, ct);

        if (viaje is null)
        {
            return catalogo.Texto("bot.calificar.sin_viajes");
        }

        await servicioViajes.CalificarAsync(viaje.Id, new CalificarViajeRequest(puntaje, null), ct);

        return catalogo.Texto("bot.calificar.gracias");
    }

    private async Task<string> AtenderConductorAsync(Usuario usuario, string texto, CancellationToken ct)
    {
        var conductor = await flota.ObtenerConductorPorUsuarioAsync(usuario.Id, ct) ?? throw new NotFoundException("conductor.sin_perfil");
        var partes = texto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var comando = partes.Length == 0 ? string.Empty : partes[0].ToUpperInvariant();

        switch (comando)
        {
            case "ACEPTAR" or "RECHAZAR":
                var pendiente = await asignaciones.ObtenerPendienteDeConductorAsync(conductor.Id, ct)
                    ?? throw new NotFoundException("conductor.sin_servicios");

                if (comando == "ACEPTAR")
                {
                    await servicioAsignaciones.AceptarAsync(pendiente.Id, ct);

                    return catalogo.Texto("conductor.aceptado");
                }

                await servicioAsignaciones.RechazarAsync(pendiente.Id, ct);

                return catalogo.Texto("conductor.rechazado");

            case "INICIO":
                await servicioViajes.IniciarAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, ct);

                return catalogo.Texto("conductor.iniciado");

            case "FIN":
                await servicioViajes.FinalizarAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, ct);

                return catalogo.Texto("conductor.finalizado");

            case "INCIDENTE":
                var descripcion = partes.Length > 1 ? partes[1] : string.Empty;
                await servicioViajes.ReportarIncidenteAsync((await ViajeVigenteAsync(conductor.Id, ct)).Id, new ReportarIncidenteRequest(descripcion), ct);

                return catalogo.Texto("conductor.incidente");

            default:
                return catalogo.Texto("conductor.ayuda");
        }
    }

    private async Task<FlowRide.Viajes.Dominio.Viaje> ViajeVigenteAsync(Guid conductorId, CancellationToken ct) =>
        await viajes.ObtenerVigenteDeConductorAsync(conductorId, ct) ?? throw new NotFoundException("conductor.sin_viaje");

    [GeneratedRegex(@"^calificar\s+([1-5])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CalificarRegex();
}
