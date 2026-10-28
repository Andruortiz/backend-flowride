using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Plataforma.Aplicacion;

internal sealed class ServicioParametros(IParametroRepository repositorio, IUnitOfWork unidad, IContextoActor actor, TimeProvider reloj)
    : IServicioParametros
{
    public async Task<int> ValorAsync(string clave, CancellationToken ct = default)
    {
        var definicion = CatalogoParametros.Obtener(clave);
        var parametro = await repositorio.ObtenerAsync(clave, ct);

        return parametro?.Valor ?? definicion.Defecto;
    }

    public async Task<IReadOnlyList<ParametroResponse>> ListarAsync(CancellationToken ct = default)
    {
        var guardados = (await repositorio.ListarAsync(ct)).ToDictionary(p => p.Clave);

        return
        [
            .. CatalogoParametros.Definiciones.Select(d =>
            {
                guardados.TryGetValue(d.Clave, out var p);
                return new ParametroResponse(d.Clave, d.Descripcion, d.Unidad, d.Minimo, d.Maximo, d.Defecto, p?.Valor ?? d.Defecto, p?.Version ?? 0);
            }),
        ];
    }

    public async Task<ParametroResponse> ActualizarAsync(string clave, ActualizarParametroRequest request, CancellationToken ct = default)
    {
        var definicion = CatalogoParametros.Obtener(clave);
        var ahora = reloj.GetUtcNow();
        var parametro = await repositorio.ObtenerAsync(clave, ct);

        if (parametro is null)
        {
            parametro = ParametroOperador.Crear(clave, request.Valor, ahora);
            await repositorio.AgregarAsync(parametro, ct);
        }
        else
        {
            parametro.Actualizar(request.Valor, actor.UsuarioId, ahora);
        }

        await unidad.GuardarCambiosAsync(ct);

        return new ParametroResponse(definicion.Clave, definicion.Descripcion, definicion.Unidad, definicion.Minimo, definicion.Maximo, definicion.Defecto, parametro.Valor, parametro.Version);
    }
}

internal sealed class ServicioAuditoria(IAuditoriaRepository repositorio, IUnitOfWork unidad, IContextoActor actor, TimeProvider reloj)
    : IServicioAuditoria
{
    public Task RegistrarAsync(string accion, string recurso, string? motivo = null, string? antes = null, string? despues = null, CancellationToken ct = default) =>
        repositorio.AgregarAsync(
            RegistroAuditoria.Registrar(actor.UsuarioId, actor.Nombre, accion, recurso, ResultadoAuditoria.Aceptada, reloj.GetUtcNow(), motivo, antes, despues),
            ct);

    public async Task RegistrarRechazoAsync(string accion, string recurso, string motivo, CancellationToken ct = default)
    {
        await repositorio.AgregarAsync(
            RegistroAuditoria.Registrar(actor.UsuarioId, actor.Nombre, accion, recurso, ResultadoAuditoria.Rechazada, reloj.GetUtcNow(), motivo),
            ct);
        await unidad.GuardarCambiosAsync(ct);
    }

    public async Task<IReadOnlyList<AuditoriaResponse>> ConsultarAsync(
        string? accion, string? recurso, DateTimeOffset? desde, DateTimeOffset? hasta, int pagina, int tamano, CancellationToken ct = default) =>
        [.. (await repositorio.ConsultarAsync(accion, recurso, desde, hasta, pagina, tamano, ct)).Select(r => r.A())];
}

internal sealed class ServicioOperador(IOperadorRepository repositorio, IServicioAuditoria auditoria, IUnitOfWork unidad) : IServicioOperador
{
    public async Task<OperadorResponse> ObtenerAsync(CancellationToken ct = default) =>
        (await repositorio.ObtenerAsync(ct) ?? throw new NotFoundException("El operador aún no está configurado.")).A();

    public async Task<OperadorResponse> ActualizarAsync(ActualizarOperadorRequest request, CancellationToken ct = default)
    {
        var operador = await repositorio.ObtenerAsync(ct) ?? throw new NotFoundException("El operador aún no está configurado.");
        operador.ActualizarDatos(request.RazonSocial, request.Nit, request.CorreoContacto, request.ZonaHoraria);
        await auditoria.RegistrarAsync("operador.actualizar", $"operador/{operador.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return operador.A();
    }

    public async Task<TimeZoneInfo> ZonaAsync(CancellationToken ct = default) =>
        (await repositorio.ObtenerAsync(ct))?.ObtenerZona() ?? TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
}
