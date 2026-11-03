using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Identidad.Aplicacion.Puertos;

namespace FlowRide.Identidad.Aplicacion;

internal sealed class ServicioPasajeros(IPasajeroRepository pasajeros, IUsuarioRepository usuarios) : IServicioPasajeros
{
    public async Task<IReadOnlyList<PasajeroResponse>> ListarAsync(CancellationToken ct = default)
    {
        var lista = await pasajeros.ListarAsync(ct);
        var porUsuario = await usuarios.ObtenerVariosAsync(lista.Select(p => p.UsuarioId), ct);

        return
        [
            .. lista
                .Where(p => porUsuario.ContainsKey(p.UsuarioId))
                .Select(p => new PasajeroResponse(p.Id, p.UsuarioId, p.ClienteEmpresarialId, porUsuario[p.UsuarioId].Nombre, porUsuario[p.UsuarioId].Telefono)),
        ];
    }
}
