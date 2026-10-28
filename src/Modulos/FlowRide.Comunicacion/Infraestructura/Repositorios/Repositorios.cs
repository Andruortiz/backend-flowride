using Microsoft.EntityFrameworkCore;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Persistencia;
using FlowRide.Comunicacion.Dominio;
using FlowRide.Comunicacion.Aplicacion;
using FlowRide.Comunicacion.Aplicacion.Dtos;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Comunicacion.Infraestructura;
using FlowRide.Comunicacion.Infraestructura.Controladores;
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

namespace FlowRide.Comunicacion.Infraestructura.Repositorios;

internal sealed class AlertaRepository(FlowRideDbContext db) : IAlertaRepository
{
    public Task<AlertaOperativa?> ObtenerAsync(Guid id, CancellationToken ct = default) => db.Set<AlertaOperativa>().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExisteClaveAsync(string claveDeduplicacion, CancellationToken ct = default) =>
        db.Set<AlertaOperativa>().AnyAsync(a => a.ClaveDeduplicacion == claveDeduplicacion, ct);

    public async Task<IReadOnlyList<AlertaOperativa>> ListarAsync(bool soloNoLeidas, int pagina, int tamano, CancellationToken ct = default) =>
        await db.Set<AlertaOperativa>()
            .Where(a => !soloNoLeidas || a.LeidaEn == null)
            .OrderByDescending(a => a.CreadaEn).ThenBy(a => a.Id)
            .Skip(Paginacion.Saltar(pagina, tamano)).Take(Paginacion.Tomar(tamano))
            .ToListAsync(ct);

    public Task<int> ContarNoLeidasAsync(CancellationToken ct = default) => db.Set<AlertaOperativa>().CountAsync(a => a.LeidaEn == null, ct);

    public async Task AgregarAsync(AlertaOperativa alerta, CancellationToken ct = default) => await db.Set<AlertaOperativa>().AddAsync(alerta, ct);
}

internal sealed class ConversacionRepository(FlowRideDbContext db) : IConversacionRepository
{
    public Task<ConversacionBot?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default) =>
        db.Set<ConversacionBot>().FirstOrDefaultAsync(c => c.UsuarioId == usuarioId, ct);

    public async Task AgregarAsync(ConversacionBot conversacion, CancellationToken ct = default) => await db.Set<ConversacionBot>().AddAsync(conversacion, ct);
}

internal sealed class MensajeEntranteRepository(FlowRideDbContext db) : IMensajeEntranteRepository
{
    public Task<bool> ExisteAsync(string externalMessageId, CancellationToken ct = default) =>
        db.Set<MensajeEntranteRegistrado>().AnyAsync(m => m.ExternalMessageId == externalMessageId, ct);

    public async Task RegistrarAsync(string externalMessageId, DateTimeOffset recibidoEn, CancellationToken ct = default) =>
        await db.Set<MensajeEntranteRegistrado>().AddAsync(new MensajeEntranteRegistrado { ExternalMessageId = externalMessageId, RecibidoEn = recibidoEn }, ct);
}

internal sealed class NotificacionEnviadaRepository(FlowRideDbContext db) : INotificacionEnviadaRepository
{
    public Task<bool> ExisteAsync(Guid eventoId, Guid destinatarioId, CanalNotificacion canal, CancellationToken ct = default)
    {
        var nombre = canal.ToString();

        return db.Set<NotificacionEnviadaRegistro>().AnyAsync(n => n.EventoId == eventoId && n.DestinatarioId == destinatarioId && n.Canal == nombre, ct);
    }

    public async Task RegistrarAsync(Guid eventoId, Guid destinatarioId, CanalNotificacion canal, string clave, DateTimeOffset enviadaEn, CancellationToken ct = default) =>
        await db.Set<NotificacionEnviadaRegistro>().AddAsync(
            new NotificacionEnviadaRegistro { EventoId = eventoId, DestinatarioId = destinatarioId, Canal = canal.ToString(), Clave = clave, EnviadaEn = enviadaEn }, ct);
}
