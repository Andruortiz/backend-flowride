using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Flota.Aplicacion.Dtos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

using FlowRide.Plataforma.Aplicacion;

namespace FlowRide.Flota.Aplicacion;

internal sealed class ServicioFlota(
    IConductorRepository conductores,
    IVehiculoRepository vehiculos,
    IConsultaIdentidad identidad,
    IServicioParametros parametros,
    IServicioOperador operador,
    IServicioAuditoria auditoria,
    IUnitOfWork unidad,
    TimeProvider reloj) : IServicioFlota
{
    public async Task<ConductorResponse> CrearConductorAsync(CrearConductorRequest request, CancellationToken ct = default)
    {
        var usuario = await identidad.ObtenerUsuarioAsync(request.UsuarioId, ct) ?? throw new NotFoundException("El usuario no existe.");

        if (await conductores.ObtenerPorUsuarioAsync(usuario.Id, ct) is not null)
        {
            throw new ConflictException("El usuario ya es conductor.");
        }

        var licencia = DocumentoVigencia.Crear(request.Licencia.Tipo, request.Licencia.Numero, request.Licencia.VenceEl);
        var conductor = Conductor.Crear(usuario, licencia);
        await conductores.AgregarAsync(conductor, ct);
        await auditoria.RegistrarAsync("conductor.crear", $"conductor/{conductor.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return await Responder(conductor, usuario, ct);
    }

    public async Task<ConductorResponse> ObtenerConductorAsync(Guid id, CancellationToken ct = default) =>
        await Responder(await BuscarConductor(id, ct), null, ct);

    public async Task<IReadOnlyList<ConductorResponse>> ListarConductoresAsync(bool soloActivos, CancellationToken ct = default)
    {
        var lista = await conductores.ListarAsync(soloActivos, ct);
        var nombres = await identidad.ObtenerUsuariosAsync(lista.Select(c => c.UsuarioId), ct);
        var (hoy, aviso) = await ContextoVigencia(ct);

        return [.. lista.Select(c => A(c, nombres.TryGetValue(c.UsuarioId, out var u) ? u.Nombre : string.Empty, hoy, aviso))];
    }

    public Task<ConductorResponse> RenovarLicenciaAsync(Guid id, DocumentoRequest request, CancellationToken ct = default) =>
        Modificar(id, "conductor.renovar_licencia", c => c.ActualizarLicencia(DocumentoVigencia.Crear(request.Tipo, request.Numero, request.VenceEl)), ct);

    public Task<ConductorResponse> DefinirTurnosAsync(Guid id, DefinirTurnosRequest request, CancellationToken ct = default) =>
        Modificar(id, "conductor.definir_turnos", c => c.DefinirTurnos(request.Turnos.Select(t => Turno.Crear(t.Dia, t.Inicio, t.Fin))), ct);

    public Task<ConductorResponse> MarcarNoDisponibleAsync(Guid id, MarcarNoDisponibleRequest request, CancellationToken ct = default) =>
        Modificar(id, "conductor.no_disponible", c => c.MarcarNoDisponible(request.Motivo), ct);

    public Task<ConductorResponse> MarcarDisponibleAsync(Guid id, CancellationToken ct = default) =>
        Modificar(id, "conductor.disponible", c => c.MarcarDisponible(), ct);

    public Task<ConductorResponse> CambiarEstadoConductorAsync(Guid id, bool activo, CancellationToken ct = default) =>
        Modificar(id, activo ? "conductor.activar" : "conductor.inactivar", c => { if (activo) { c.Activar(); } else { c.Inactivar(); } }, ct);

    public async Task<ConductorResponse> AsignarVehiculoAsync(Guid id, AsignarVehiculoRequest request, CancellationToken ct = default)
    {
        var vehiculo = await vehiculos.ObtenerAsync(request.VehiculoId, ct) ?? throw new NotFoundException("El vehículo no existe.");

        return await Modificar(id, "conductor.asignar_vehiculo", c => c.AsignarVehiculo(vehiculo), ct);
    }

    public Task<ConductorResponse> QuitarVehiculoAsync(Guid id, CancellationToken ct = default) =>
        Modificar(id, "conductor.quitar_vehiculo", c => c.QuitarVehiculo(), ct);

    public async Task<VehiculoResponse> CrearVehiculoAsync(CrearVehiculoRequest request, CancellationToken ct = default)
    {
        var vehiculo = Vehiculo.Crear(request.Placa, request.Capacidad);

        if (await vehiculos.ObtenerPorPlacaAsync(vehiculo.Placa, ct) is not null)
        {
            throw new ConflictException("Ya existe un vehículo con esa placa.");
        }

        await vehiculos.AgregarAsync(vehiculo, ct);
        await auditoria.RegistrarAsync("vehiculo.crear", $"vehiculo/{vehiculo.Id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return await ResponderVehiculo(vehiculo, ct);
    }

    public async Task<VehiculoResponse> ObtenerVehiculoAsync(Guid id, CancellationToken ct = default) =>
        await ResponderVehiculo(await BuscarVehiculo(id, ct), ct);

    public async Task<IReadOnlyList<VehiculoResponse>> ListarVehiculosAsync(CancellationToken ct = default)
    {
        var (hoy, aviso) = await ContextoVigencia(ct);

        return [.. (await vehiculos.ListarAsync(ct)).Select(v => v.A(hoy, aviso))];
    }

    public async Task<VehiculoResponse> RegistrarDocumentoVehiculoAsync(Guid id, DocumentoRequest request, CancellationToken ct = default)
    {
        var vehiculo = await BuscarVehiculo(id, ct);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(request.Tipo, request.Numero, request.VenceEl));
        await auditoria.RegistrarAsync("vehiculo.documento", $"vehiculo/{id}", despues: request.Tipo.ToString(), ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return await ResponderVehiculo(vehiculo, ct);
    }

    public async Task<VehiculoResponse> CambiarHabilitacionVehiculoAsync(Guid id, bool habilitado, CancellationToken ct = default)
    {
        var vehiculo = await BuscarVehiculo(id, ct);

        if (habilitado)
        {
            vehiculo.Habilitar();
        }
        else
        {
            vehiculo.Deshabilitar();
        }

        await auditoria.RegistrarAsync(habilitado ? "vehiculo.habilitar" : "vehiculo.deshabilitar", $"vehiculo/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return await ResponderVehiculo(vehiculo, ct);
    }

    private async Task<ConductorResponse> Modificar(Guid id, string accion, Action<Conductor> cambio, CancellationToken ct)
    {
        var conductor = await BuscarConductor(id, ct);
        cambio(conductor);
        await auditoria.RegistrarAsync(accion, $"conductor/{id}", ct: ct);
        await unidad.GuardarCambiosAsync(ct);

        return await Responder(conductor, null, ct);
    }

    private async Task<Conductor> BuscarConductor(Guid id, CancellationToken ct) =>
        await conductores.ObtenerAsync(id, ct) ?? throw new NotFoundException("El conductor no existe.");

    private async Task<Vehiculo> BuscarVehiculo(Guid id, CancellationToken ct) =>
        await vehiculos.ObtenerAsync(id, ct) ?? throw new NotFoundException("El vehículo no existe.");

    private async Task<(DateOnly Hoy, int Aviso)> ContextoVigencia(CancellationToken ct) =>
        (Fechas.Hoy(reloj, await operador.ZonaAsync(ct)), await parametros.ValorAsync(CatalogoParametros.DiasAvisoDocumentos, ct));

    private async Task<VehiculoResponse> ResponderVehiculo(Vehiculo vehiculo, CancellationToken ct)
    {
        var (hoy, aviso) = await ContextoVigencia(ct);

        return vehiculo.A(hoy, aviso);
    }

    private async Task<ConductorResponse> Responder(Conductor conductor, Usuario? usuario, CancellationToken ct)
    {
        usuario ??= await identidad.ObtenerUsuarioAsync(conductor.UsuarioId, ct);
        var (hoy, aviso) = await ContextoVigencia(ct);

        return A(conductor, usuario?.Nombre ?? string.Empty, hoy, aviso);
    }

    private static ConductorResponse A(Conductor c, string nombre, DateOnly hoy, int aviso) =>
        new(c.Id, c.UsuarioId, nombre, c.Activo, c.Disponible, c.MotivoNoDisponible, c.VehiculoId, c.Licencia.A(hoy, aviso),
            [.. c.Turnos.Select(t => new TurnoResponse(t.Dia, t.Inicio, t.Fin))]);
}
