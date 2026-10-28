using Microsoft.EntityFrameworkCore;

namespace FlowRide.Persistencia;

/// <summary>
/// Cada módulo describe sus tablas con una implementación de esta interfaz; el DbContext único las compone.
/// Así el módulo no conoce a los demás y Persistencia no conoce a ningún módulo.
/// </summary>
public interface IConfiguradorModelo
{
    /// <summary>Entidades de dominio del módulo (las que heredan de <c>Entidad</c>).</summary>
    IReadOnlyList<Type> Entidades { get; }

    void Configurar(ModelBuilder modelo);
}
