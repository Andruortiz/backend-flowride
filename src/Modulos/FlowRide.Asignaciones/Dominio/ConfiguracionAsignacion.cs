using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Solicitudes.Dominio;
using FlowRide.Viajes.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Asignaciones.Dominio;

public sealed record ConfiguracionRegla(string ReglaId, bool Activa, int Orden, double Peso);

public sealed class ConfiguracionAsignacion : Entidad
{
    private ConfiguracionAsignacion(Guid id, int version, IReadOnlyList<ConfiguracionRegla> reglas, Guid? creadaPor, DateTimeOffset creadaEn)
        : base(id)
    {
        Version = version;
        Reglas = reglas;
        CreadaPor = creadaPor;
        CreadaEn = creadaEn;
    }

    public int Version { get; }

    public IReadOnlyList<ConfiguracionRegla> Reglas { get; }

    public Guid? CreadaPor { get; }

    public DateTimeOffset CreadaEn { get; }

    public static ConfiguracionAsignacion PorDefecto(DateTimeOffset ahora) =>
        Crear(
            1,
            [
                new("disponibilidad", true, 1, 1),
                new("turno", true, 2, 1),
                new("regulatoria", true, 3, 1),
                new("capacidad", true, 4, 1),
                new("urgente", true, 5, 0.5),
                new("balance_carga", true, 6, 0.6),
                new("recientes", true, 7, 0.4),
            ],
            null,
            ahora);

    public static ConfiguracionAsignacion Crear(
        int version,
        IEnumerable<ConfiguracionRegla> reglas,
        Guid? creadaPor,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(reglas);

        if (version < 1)
        {
            throw new DomainException("error.la_version_de_la_configuracion_debe_ser");
        }

        var lista = reglas.OrderBy(r => r.Orden).ToList();
        ValidadorConfiguracion.Validar(lista, CatalogoReglas.Todas);

        return new ConfiguracionAsignacion(UuidHelper.GetOrGenerate(id), version, lista, creadaPor, ahora);
    }
}

public static class ValidadorConfiguracion
{
    public static void Validar(IReadOnlyList<ConfiguracionRegla> reglas, IReadOnlyList<IAssignmentRule> catalogo)
    {
        var errores = new List<MensajeClave>();
        var porId = catalogo.ToDictionary(r => r.Id);

        foreach (var duplicada in reglas.GroupBy(r => r.ReglaId).Where(g => g.Count() > 1))
        {
            errores.Add(new MensajeClave("error.la_regla_esta_repetida", ("regla", duplicada.Key)));
        }

        foreach (var regla in reglas)
        {
            if (!porId.TryGetValue(regla.ReglaId, out var definicion))
            {
                errores.Add(new MensajeClave("error.la_regla_no_existe", ("regla", regla.ReglaId)));
                continue;
            }

            if (regla.Peso is < 0 or > 1)
            {
                errores.Add(new MensajeClave("error.el_peso_de_la_regla_fuera_de_rango", ("regla", definicion.Nombre)));
            }

            if (!regla.Activa && definicion.Obligatoria)
            {
                errores.Add(new MensajeClave("error.la_regla_es_obligatoria", ("regla", definicion.Nombre)));
            }
        }

        foreach (var obligatoria in catalogo.Where(r => r.Obligatoria))
        {
            if (!reglas.Any(r => r.ReglaId == obligatoria.Id))
            {
                errores.Add(new MensajeClave("error.falta_la_regla_obligatoria", ("regla", obligatoria.Nombre)));
            }
        }

        if (reglas.GroupBy(r => r.Orden).Any(g => g.Count() > 1))
        {
            errores.Add(new MensajeClave("error.dos_reglas_no_pueden_tener_el_mismo_orden"));
        }

        var puntuaActiva = reglas.Any(r => r.Activa && porId.TryGetValue(r.ReglaId, out var d) && d.Tipo == TipoRegla.Puntuacion);
        if (!puntuaActiva)
        {
            errores.Add(new MensajeClave("error.debe_haber_una_regla_de_puntuacion_activa"));
        }

        if (errores.Count > 0)
        {
            throw new DomainException(errores);
        }
    }
}
