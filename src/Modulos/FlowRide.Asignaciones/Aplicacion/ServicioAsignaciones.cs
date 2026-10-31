using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Asignaciones.Aplicacion.Dtos;
using FlowRide.Asignaciones.Aplicacion.Puertos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Viajes.Dominio;
using FlowRide.Viajes.Aplicacion.Dtos;
using FlowRide.Viajes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Asignaciones.Aplicacion;

internal sealed class ServicioAsignaciones(
    IConsultaSolicitudes solicitudes,
    IAsignacionRepository asignaciones,
    IConsultaFlota flota,
    IProgramacionViajes viajes,
    IConsultaClientes clientes,
    IConfiguracionAsignacionRepository configuraciones,
    IServicioParametros parametros,
    IServicioOperador operador,
    IServicioAuditoria auditoria,
    IContextoActor actor,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioAsignaciones
{
    private static readonly AssignmentEngine Motor = new();

    public async Task<ResultadoAsignacionResponse?> AsignarAutomaticoAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(solicitudId, ct) ?? throw new NotFoundException("La solicitud no existe.");

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return null;
        }

        var ahora = reloj.GetUtcNow();
        var configuracion = await ConfiguracionVigenteAsync(ahora, ct);
        var (resultado, _) = await EvaluarAsync(solicitud, configuracion, ct);

        Asignacion? asignacion = null;

        if (resultado.Ganador is { } ganador)
        {
            asignacion = Asignacion.Crear(
                solicitud.Id, ganador.Conductor.Id, ganador.Vehiculo!.Id, configuracion.Version, resultado.Decision, ahora);
            await asignaciones.AgregarAsync(asignacion, ct);
            solicitud.MarcarAsignada(asignacion.Id);
        }
        else
        {
            solicitud.MarcarEnRevision("Ningún conductor cumple las reglas de asignación.", ahora);
        }

        await auditoria.RegistrarAsync(
            asignacion is null ? "asignacion.sin_recurso" : "asignacion.automatica", $"solicitud/{solicitudId}", despues: asignacion?.Id.ToString(), ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return new ResultadoAsignacionResponse(asignacion is not null, asignacion?.A(), resultado.Decision, solicitud.Estado);
    }

    public async Task<ResultadoAsignacionResponse> ReasignarManualAsync(Guid solicitudId, ReasignarManualRequest request, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(solicitudId, ct) ?? throw new NotFoundException("La solicitud no existe.");
        var conductor = await flota.ObtenerConductorAsync(request.ConductorId, ct) ?? throw new NotFoundException("El conductor no existe.");
        var vehiculo = conductor.VehiculoId is { } vehiculoId ? await flota.ObtenerVehiculoAsync(vehiculoId, ct) : null;

        var zona = await operador.ZonaAsync(ct);
        var local = TimeZoneInfo.ConvertTime(solicitud.FechaHoraProgramada, zona);
        var evaluacion = conductor.EvaluarAsignabilidad(vehiculo, DateOnly.FromDateTime(local.DateTime), TimeOnly.FromDateTime(local.DateTime));

        if (!evaluacion.EsAsignable)
        {
            await auditoria.RegistrarRechazoAsync(
                "asignacion.manual", $"solicitud/{solicitudId}", $"El conductor no es asignable: {string.Join(", ", evaluacion.Motivos)}", ct);

            throw new DomainException($"El conductor no es asignable: {string.Join(", ", evaluacion.Motivos)}.");
        }

        var ahora = reloj.GetUtcNow();
        var configuracion = await ConfiguracionVigenteAsync(ahora, ct);
        var nueva = Asignacion.CrearManual(solicitud.Id, conductor.Id, vehiculo!.Id, configuracion.Version, request.Motivo, ahora);
        var anterior = await asignaciones.ObtenerActivaPorSolicitudAsync(solicitudId, ct);

        if (anterior is not null)
        {
            anterior.Reasignar(nueva, request.Motivo, ahora);

            await viajes.CancelarProgramadoDeAsignacionAsync(anterior.Id, ahora, ct);

            await asignaciones.AgregarAsync(nueva, ct);
            solicitud.CambiarAsignacion(nueva.Id);
        }
        else
        {
            await asignaciones.AgregarAsync(nueva, ct);
            solicitud.MarcarAsignada(nueva.Id);
        }

        await auditoria.RegistrarAsync(
            "asignacion.manual", $"solicitud/{solicitudId}", request.Motivo, anterior?.ConductorId.ToString(), nueva.ConductorId.ToString(), ct);
        await unidad.GuardarCambiosAsync(ct);

        var decision = new DecisionAsignacion(configuracion.Version, [], [], [], conductor.Id, "manual");

        return new ResultadoAsignacionResponse(true, nueva.A(), decision, solicitud.Estado);
    }

    public async Task<AsignacionResponse> AceptarAsync(Guid asignacionId, CancellationToken ct = default)
    {
        var asignacion = await BuscarParaConductorAsync(asignacionId, ct);
        var solicitud = await solicitudes.ObtenerAsync(asignacion.SolicitudId, ct) ?? throw new NotFoundException("La solicitud no existe.");
        var ahora = reloj.GetUtcNow();

        asignacion.Aceptar(ahora);

        var pasajeroId = solicitud.PasajeroId;
        var programado = solicitud.FechaHoraProgramada;
        var duracion = await DuracionAsync(solicitud, ct);

        await viajes.ProgramarAsync(
            solicitud.Id, asignacion.Id, pasajeroId, asignacion.ConductorId, asignacion.VehiculoId, programado, duracion,
            solicitud.DestinoLatitud, solicitud.DestinoLongitud, ahora, ct);

        await auditoria.RegistrarAsync("asignacion.aceptar", $"asignacion/{asignacionId}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return asignacion.A();
    }

    public async Task<AsignacionResponse> RechazarAsync(Guid asignacionId, CancellationToken ct = default)
    {
        var asignacion = await BuscarParaConductorAsync(asignacionId, ct);
        var solicitud = await solicitudes.ObtenerAsync(asignacion.SolicitudId, ct) ?? throw new NotFoundException("La solicitud no existe.");
        var ahora = reloj.GetUtcNow();

        asignacion.Rechazar(ahora);
        solicitud.VolverAPendiente();

        var maximo = await parametros.ValorAsync(CatalogoParametros.MaximoIntentosAsignacion, ct);

        if (solicitud.Intentos >= maximo)
        {
            solicitud.MarcarEnRevision($"Se agotaron {maximo} intentos de asignación.", ahora);
        }

        await auditoria.RegistrarAsync("asignacion.rechazar", $"asignacion/{asignacionId}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return asignacion.A();
    }

    public async Task<SimulacionAsignacionResponse> SimularAsync(Guid solicitudId, CancellationToken ct = default)
    {
        var solicitud = await solicitudes.ObtenerAsync(solicitudId, ct) ?? throw new NotFoundException("La solicitud no existe.");
        var configuracion = await ConfiguracionVigenteAsync(reloj.GetUtcNow(), ct, persistir: false);
        var (resultado, _) = await EvaluarAsync(solicitud, configuracion, ct);

        return new SimulacionAsignacionResponse(resultado.Ganador?.Conductor.Id, resultado.Decision);
    }

    public async Task<AsignacionResponse> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        (await asignaciones.ObtenerAsync(id, ct) ?? throw new NotFoundException("La asignación no existe.")).A();

    public async Task<IReadOnlyList<AsignacionResponse>> ListarAsync(Guid? conductorId, EstadoAsignacion? estado, int pagina, int tamano, CancellationToken ct = default)
    {
        if (actor.Rol == RolUsuario.Conductor)
        {
            conductorId = (await ConductorActualAsync(ct)).Id;
        }

        return [.. (await asignaciones.ListarAsync(conductorId, estado, pagina, tamano, ct)).Select(a => a.A())];
    }

    public async Task<ConfiguracionAsignacionResponse> ObtenerConfiguracionAsync(CancellationToken ct = default) =>
        A(await ConfiguracionVigenteAsync(reloj.GetUtcNow(), ct, persistir: false));

    public async Task<ConfiguracionAsignacionResponse> ActualizarConfiguracionAsync(ActualizarConfiguracionAsignacionRequest request, CancellationToken ct = default)
    {
        var ahora = reloj.GetUtcNow();
        var actual = await ConfiguracionVigenteAsync(ahora, ct, persistir: false);
        var nueva = ConfiguracionAsignacion.Crear(
            actual.Version + 1, request.Reglas.Select(r => new ConfiguracionRegla(r.ReglaId, r.Activa, r.Orden, r.Peso)), actor.UsuarioId, ahora);

        await configuraciones.AgregarAsync(nueva, ct);
        await auditoria.RegistrarAsync("asignacion.configuracion", $"configuracion/{nueva.Version}", antes: $"{actual.Version}", despues: $"{nueva.Version}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return A(nueva);
    }

    private static ConfiguracionAsignacionResponse A(ConfiguracionAsignacion c) =>
        new(c.Version, c.CreadaEn,
        [
            .. c.Reglas.Select(r =>
            {
                var regla = CatalogoReglas.Todas.First(x => x.Id == r.ReglaId);
                return new ReglaConfiguradaResponse(r.ReglaId, regla.Nombre, regla.Tipo, regla.Obligatoria, r.Activa, r.Orden, r.Peso);
            }),
        ]);

    private async Task<ConfiguracionAsignacion> ConfiguracionVigenteAsync(DateTimeOffset ahora, CancellationToken ct, bool persistir = true)
    {
        var existente = await configuraciones.ObtenerActivaAsync(ct);

        if (existente is not null)
        {
            return existente;
        }

        var porDefecto = ConfiguracionAsignacion.PorDefecto(ahora);

        if (persistir)
        {
            await configuraciones.AgregarAsync(porDefecto, ct);
        }

        return porDefecto;
    }

    private async Task<(ResultadoAsignacion Resultado, int Candidatos)> EvaluarAsync(
        SolicitudTransporte solicitud, ConfiguracionAsignacion configuracion, CancellationToken ct)
    {
        var activos = await flota.ListarConductoresAsync(true, ct);
        var descartados = await asignaciones.ConductoresDescartadosAsync(solicitud.Id, ct);
        var elegibles = activos.Where(c => !descartados.Contains(c.Id)).ToList();
        var ids = elegibles.Select(c => c.Id).ToList();

        var ventana = await parametros.ValorAsync(CatalogoParametros.VentanaCargaHoras, ct);
        var cargas = await asignaciones.ContarActivasPorConductorAsync(ids, ct);
        var recientes = await asignaciones.ContarRecientesPorConductorAsync(ids, reloj.GetUtcNow().AddHours(-ventana), ct);
        var vehiculosPorId = await flota.ObtenerVehiculosAsync(elegibles.Where(c => c.VehiculoId is not null).Select(c => c.VehiculoId!.Value), ct);

        var candidatos = elegibles
            .Select(c => new CandidatoAsignacion(
                c,
                c.VehiculoId is { } v && vehiculosPorId.TryGetValue(v, out var vehiculo) ? vehiculo : null,
                cargas.GetValueOrDefault(c.Id),
                recientes.GetValueOrDefault(c.Id)))
            .ToList();

        var zona = await operador.ZonaAsync(ct);
        var local = TimeZoneInfo.ConvertTime(solicitud.FechaHoraProgramada, zona);
        var contexto = new ContextoAsignacion(solicitud, DateOnly.FromDateTime(local.DateTime), TimeOnly.FromDateTime(local.DateTime));

        return (Motor.Asignar(contexto, candidatos, configuracion), candidatos.Count);
    }

    private async Task<TimeSpan> DuracionAsync(SolicitudTransporte solicitud, CancellationToken ct)
    {
        if (solicitud.ItinerarioId is { } itinerarioId && await clientes.ObtenerItinerarioAsync(itinerarioId, ct) is { } itinerario)
        {
            var minutos = (itinerario.HoraLlegada.ToTimeSpan() - itinerario.HoraSalida.ToTimeSpan()).TotalMinutes;

            if (minutos > 0)
            {
                return TimeSpan.FromMinutes(minutos);
            }
        }

        return TimeSpan.FromMinutes(await parametros.ValorAsync(CatalogoParametros.DuracionViajeMinutos, ct));
    }

    private async Task<Conductor> ConductorActualAsync(CancellationToken ct) =>
        actor.UsuarioId is { } usuarioId && await flota.ObtenerConductorPorUsuarioAsync(usuarioId, ct) is { } conductor
            ? conductor
            : throw new ForbiddenException("El usuario autenticado no es un conductor.");

    private async Task<Asignacion> BuscarParaConductorAsync(Guid asignacionId, CancellationToken ct)
    {
        var asignacion = await asignaciones.ObtenerAsync(asignacionId, ct) ?? throw new NotFoundException("La asignación no existe.");

        if ((await ConductorActualAsync(ct)).Id != asignacion.ConductorId)
        {
            throw new ForbiddenException("Esta asignación no es tuya.");
        }

        return asignacion;
    }
}
