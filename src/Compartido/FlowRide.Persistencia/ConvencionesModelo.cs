using System.Reflection;
using FlowRide.Nucleo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Persistencia;

internal static class ConvencionesModelo
{
    public static void MapearPropiedadesDeSoloLectura(ModelBuilder modelo, IEnumerable<Type> entidades)
    {
        foreach (var tipo in entidades)
        {
            var entidad = modelo.Entity(tipo);
            entidad.HasKey(nameof(Entidad.Id));

            foreach (var propiedad in tipo.GetProperties())
            {
                var respaldo = propiedad.DeclaringType!.GetField(
                    $"<{propiedad.Name}>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (respaldo is not null && propiedad.SetMethod is null && EsEscalar(propiedad.PropertyType))
                {
                    entidad.Property(propiedad.Name);
                }
            }
        }
    }

    public static void Aplicar(ModelBuilder modelo)
    {
        foreach (var tipo in modelo.Model.GetEntityTypes().ToList())
        {
            if (typeof(RaizAgregado).IsAssignableFrom(tipo.ClrType))
            {
                modelo.Entity(tipo.ClrType).Ignore(nameof(RaizAgregado.EventosDominio));
            }

            if (typeof(Entidad).IsAssignableFrom(tipo.ClrType))
            {
                modelo.Entity(tipo.ClrType).Property<uint>("xmin").IsRowVersion();
            }

            foreach (var propiedad in tipo.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType).IsEnum))
            {
                propiedad.SetProviderClrType(typeof(string));
                propiedad.SetMaxLength(40);
            }
        }
    }

    private static bool EsEscalar(Type tipo)
    {
        var t = Nullable.GetUnderlyingType(tipo) ?? tipo;

        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(TimeSpan) || t == typeof(decimal);
    }
}
