using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
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

internal sealed class ServicioViajes(
    IViajeRepository viajes,
    IConsultaFlota flota,
    IConsultaIdentidad identidad,
    ICalificacionRepository calificaciones,
    IServicioAuditoria auditoria,
    IContextoActor actor,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioViajes
{
    public async Task<ViajeResponse> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var viaje = await Buscar(id, ct);

        if (actor.Rol == RolUsuario.Conductor && (await ConductorActualAsync(ct)).Id != viaje.ConductorId)
        {
            throw new ForbiddenException("Este viaje no es tuyo.");
        }

        return viaje.A();
    }

    public async Task<IReadOnlyList<ViajeResponse>> ListarAsync(
        EstadoViaje? estado, Guid? conductorId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default)
    {
        if (actor.Rol == RolUsuario.Conductor)
        {
            conductorId = (await ConductorActualAsync(ct)).Id;
        }

        return [.. (await viajes.ListarAsync(estado, conductorId, desde, hasta, pagina, tamano, ct)).Select(v => v.A())];
    }

    public async Task<ViajeResponse> IniciarAsync(Guid id, CancellationToken ct = default)
    {
        var viaje = await BuscarDelConductorAsync(id, ct);

        if (await viajes.ObtenerEnCursoDeVehiculoAsync(viaje.VehiculoId, ct) is { } enCurso && enCurso.Id != viaje.Id)
        {
            throw new ConflictException("El vehículo ya tiene un viaje en curso.");
        }

        // La solicitud y la asignación avanzan al recibir ViajeIniciado.
        viaje.Iniciar(reloj.GetUtcNow());

        await auditoria.RegistrarAsync("viaje.iniciar", $"viaje/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return viaje.A();
    }

    public async Task<ViajeResponse> FinalizarAsync(Guid id, CancellationToken ct = default)
    {
        var viaje = await BuscarDelConductorAsync(id, ct);
        // La solicitud y la asignación avanzan al recibir ViajeFinalizado.
        viaje.Finalizar(reloj.GetUtcNow());

        await auditoria.RegistrarAsync("viaje.finalizar", $"viaje/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return viaje.A();
    }

    public async Task<ViajeResponse> ReportarIncidenteAsync(Guid id, ReportarIncidenteRequest request, CancellationToken ct = default)
    {
        var viaje = await BuscarDelConductorAsync(id, ct);
        viaje.ReportarIncidente(request.Descripcion, reloj.GetUtcNow());
        await auditoria.RegistrarAsync("viaje.incidente", $"viaje/{id}", motivo: request.Descripcion, ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return viaje.A();
    }

    public async Task<CalificacionResponse> CalificarAsync(Guid id, CalificarViajeRequest request, CancellationToken ct = default)
    {
        var viaje = await Buscar(id, ct);
        var pasajero = actor.UsuarioId is { } usuarioId && await identidad.ObtenerPasajeroPorUsuarioAsync(usuarioId, ct) is { } p
            ? p
            : throw new ForbiddenException("Solo el pasajero califica su viaje.");

        if (await calificaciones.ExistePorViajeAsync(id, ct))
        {
            throw new ConflictException("El viaje ya fue calificado.");
        }

        var calificacion = Calificacion.Crear(viaje, pasajero.Id, request.Puntaje, request.Comentario, reloj.GetUtcNow());
        await calificaciones.AgregarAsync(calificacion, ct);
        await unidad.GuardarCambiosAsync(ct);

        return calificacion.A();
    }

    private async Task<Viaje> Buscar(Guid id, CancellationToken ct) =>
        await viajes.ObtenerAsync(id, ct) ?? throw new NotFoundException("El viaje no existe.");

    private async Task<Conductor> ConductorActualAsync(CancellationToken ct) =>
        actor.UsuarioId is { } usuarioId && await flota.ObtenerConductorPorUsuarioAsync(usuarioId, ct) is { } conductor
            ? conductor
            : throw new ForbiddenException("El usuario autenticado no es un conductor.");

    private async Task<Viaje> BuscarDelConductorAsync(Guid id, CancellationToken ct)
    {
        var viaje = await Buscar(id, ct);

        if ((await ConductorActualAsync(ct)).Id != viaje.ConductorId)
        {
            throw new ForbiddenException("Este viaje no es tuyo.");
        }

        return viaje;
    }
}
