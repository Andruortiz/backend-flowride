using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Identidad.Aplicacion;
using FlowRide.Identidad.Aplicacion.Dtos;
using FlowRide.Clientes.Dominio;
using FlowRide.Clientes.Aplicacion.Dtos;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using FlowRide.Plataforma.Aplicacion.Dtos;
using FlowRide.Plataforma.Aplicacion.Puertos;

namespace FlowRide.Identidad.Aplicacion.Puertos;

internal interface IUsuarioRepository
{
    Task<Usuario?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Usuario?> ObtenerPorTelefonoAsync(string telefono, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default);

    Task<bool> ExisteCorreoAsync(string correo, Guid? excluirId, CancellationToken ct = default);

    Task<bool> ExisteTelefonoAsync(string telefono, Guid? excluirId, CancellationToken ct = default);

    Task<IReadOnlyList<Usuario>> ListarAsync(RolUsuario? rol, bool? activo, int pagina, int tamano, CancellationToken ct = default);

    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);
}

internal interface IPasajeroRepository
{
    Task<Pasajero?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Pasajero?> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, Pasajero>> ObtenerVariosAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task AgregarAsync(Pasajero pasajero, CancellationToken ct = default);
}
