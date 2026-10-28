using FlowRide.Viajes.Dominio;

namespace FlowRide.Viajes.Aplicacion.Puertos;

/// <summary>
/// Lo que este módulo deja leer a los demás. Las entidades devueltas siguen en el contexto de la solicitud:
/// si el módulo que consulta las modifica, se guardan en la misma transacción (único caso permitido, ver ADR de módulos).
/// </summary>
public interface IConsultaViajes
{
    Task<Viaje?> ObtenerVigenteDeConductorAsync(Guid conductorId, CancellationToken ct = default);
    Task<Viaje?> ObtenerUltimoFinalizadoDePasajeroAsync(Guid pasajeroId, CancellationToken ct = default);
}
