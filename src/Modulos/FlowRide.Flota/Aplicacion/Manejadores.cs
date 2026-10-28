using FlowRide.Contratos;
using FlowRide.Flota.Aplicacion.Puertos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Persistencia;

namespace FlowRide.Flota.Aplicacion;

/// <summary>Si desactivan al usuario de un conductor, el conductor deja de recibir asignaciones.</summary>
internal sealed class AlDesactivarUsuario(IConductorRepository conductores, IUnitOfWork unidad) : IManejadorEvento<UsuarioDesactivado>
{
    public async Task ManejarAsync(UsuarioDesactivado evento, CancellationToken ct = default)
    {
        if (await conductores.ObtenerPorUsuarioAsync(evento.UsuarioId, ct) is not { Activo: true } conductor)
        {
            return;
        }

        conductor.Inactivar();
        await unidad.GuardarCambiosAsync(ct);
    }
}
