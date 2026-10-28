using FlowRide.Clientes.Dominio;

namespace FlowRide.Clientes.Aplicacion.Puertos;

/// <summary>
/// Lo que este módulo deja leer a los demás. Las entidades devueltas siguen en el contexto de la solicitud:
/// si el módulo que consulta las modifica, se guardan en la misma transacción (único caso permitido, ver ADR de módulos).
/// </summary>
public interface IConsultaClientes
{
    Task<ClienteEmpresarial?> ObtenerClienteAsync(Guid id, CancellationToken ct = default);
    Task<Itinerario?> ObtenerItinerarioAsync(Guid id, CancellationToken ct = default);
}
