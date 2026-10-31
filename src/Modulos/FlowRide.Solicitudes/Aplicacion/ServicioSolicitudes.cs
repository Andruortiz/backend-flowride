using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Solicitudes.Aplicacion.Dtos;
using FlowRide.Solicitudes.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Solicitudes.Aplicacion;

internal sealed class ServicioSolicitudes(
    ISolicitudRepository solicitudes,
    IConsultaIdentidad identidad,
    IServicioParametros parametros,
    IServicioAuditoria auditoria,
    IContextoActor actor,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioSolicitudes
{
    public async Task<SolicitudResponse> RegistrarAsync(CrearSolicitudRequest request, CancellationToken ct = default)
    {
        var pasajero = await ResolverPasajeroAsync(request.PasajeroId, ct);
        var solicitud = SolicitudTransporte.Registrar(
            pasajero,
            request.Origen,
            request.Destino,
            request.FechaHoraProgramada,
            request.Urgente,
            request.EsPersonal,
            reloj.GetUtcNow(),
            request.ItinerarioId,
            destinoLatitud: request.DestinoLatitud,
            destinoLongitud: request.DestinoLongitud);

        await solicitudes.AgregarAsync(solicitud, ct);
        await auditoria.RegistrarAsync("solicitud.registrar", $"solicitud/{solicitud.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return A(solicitud);
    }

    public async Task<SolicitudResponse> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var solicitud = await Buscar(id, ct);
        await ValidarAccesoAsync(solicitud, ct);

        return A(solicitud);
    }

    public async Task<IReadOnlyList<SolicitudResponse>> ListarAsync(
        EstadoSolicitud? estado, Guid? pasajeroId, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default)
    {
        if (actor.Rol == RolUsuario.Pasajero)
        {
            pasajeroId = (await PasajeroActualAsync(ct)).Id;
        }

        return [.. (await solicitudes.ListarAsync(estado, pasajeroId, desde, hasta, pagina, tamano, ct)).Select(A)];
    }

    public async Task<SolicitudResponse> ModificarAsync(Guid id, ModificarSolicitudRequest request, CancellationToken ct = default)
    {
        var solicitud = await Buscar(id, ct);
        await ValidarAccesoAsync(solicitud, ct);
        solicitud.Modificar(request.Origen, request.Destino, request.FechaHoraProgramada, reloj.GetUtcNow());
        await auditoria.RegistrarAsync("solicitud.modificar", $"solicitud/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return A(solicitud);
    }

    public async Task<SolicitudResponse> CancelarAsync(Guid id, CancelarSolicitudRequest request, CancellationToken ct = default)
    {
        var solicitud = await Buscar(id, ct);
        await ValidarAccesoAsync(solicitud, ct);

        var ahora = reloj.GetUtcNow();
        var ventana = await parametros.ValorAsync(CatalogoParametros.VentanaCancelacionMinutos, ct);
        var esOperador = actor.Rol is RolUsuario.AdministradorOperador or RolUsuario.OperadorLogistico;

        solicitud.Cancelar(request.Motivo, esOperador, ventana, ahora);

        await auditoria.RegistrarAsync("solicitud.cancelar", $"solicitud/{id}", motivo: request.Motivo, ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return A(solicitud);
    }

    private async Task<SolicitudTransporte> Buscar(Guid id, CancellationToken ct) =>
        await solicitudes.ObtenerAsync(id, ct) ?? throw new NotFoundException("La solicitud no existe.");

    private async Task<Pasajero> PasajeroActualAsync(CancellationToken ct) =>
        actor.UsuarioId is { } usuarioId && await identidad.ObtenerPasajeroPorUsuarioAsync(usuarioId, ct) is { } pasajero
            ? pasajero
            : throw new ForbiddenException("El usuario autenticado no es un pasajero.");

    private async Task<Pasajero> ResolverPasajeroAsync(Guid? pasajeroId, CancellationToken ct)
    {
        if (actor.Rol == RolUsuario.Pasajero)
        {
            return await PasajeroActualAsync(ct);
        }

        var id = pasajeroId ?? throw new DomainException("Indica el pasajero de la solicitud.");

        return await identidad.ObtenerPasajeroAsync(id, ct) ?? throw new NotFoundException("El pasajero no existe.");
    }

    private async Task ValidarAccesoAsync(SolicitudTransporte solicitud, CancellationToken ct)
    {
        if (actor.Rol == RolUsuario.Pasajero && (await PasajeroActualAsync(ct)).Id != solicitud.PasajeroId)
        {
            throw new ForbiddenException("Solo puedes operar tus propias solicitudes.");
        }
    }

    private SolicitudResponse A(SolicitudTransporte s)
    {
        var oculta = s.EsPersonal && actor.Rol is RolUsuario.AdministradorOperador or RolUsuario.OperadorLogistico;

        return new SolicitudResponse(
            s.Id, s.PasajeroId, oculta ? null : s.Origen, oculta ? null : s.Destino, s.FechaHoraProgramada, s.Urgente, s.EsPersonal,
            s.Estado, s.AsignacionId, s.Intentos, s.MotivoCancelacion, s.CreadaEn);
    }
}
