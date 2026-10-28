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

public interface IServicioUsuarios
{
    Task<UsuarioResponse> CrearAsync(CrearUsuarioRequest request, CancellationToken ct = default);

    Task<UsuarioResponse> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<UsuarioResponse>> ListarAsync(RolUsuario? rol, bool? activo, int pagina, int tamano, CancellationToken ct = default);

    Task<UsuarioResponse> ActualizarAsync(Guid id, ActualizarUsuarioRequest request, CancellationToken ct = default);

    Task<UsuarioResponse> CambiarRolAsync(Guid id, CambiarRolRequest request, CancellationToken ct = default);

    Task DesactivarAsync(Guid id, CancellationToken ct = default);

    Task ActivarAsync(Guid id, CancellationToken ct = default);
}
