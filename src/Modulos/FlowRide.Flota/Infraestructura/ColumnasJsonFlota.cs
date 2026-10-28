using FlowRide.Flota.Dominio;
using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowRide.Flota.Infraestructura;

internal static class ColumnasJsonFlota
{
    public static PropertyBuilder<DocumentoVigencia> ComoDocumentoJson(this PropertyBuilder<DocumentoVigencia> propiedad) =>
        propiedad
            .HasColumnType("jsonb")
            .HasConversion(
                v => ColumnasJson.Serializar(DocumentoJson.De(v)),
                s => ColumnasJson.Deserializar<DocumentoJson>(s).A());

    public static PropertyBuilder<List<DocumentoVigencia>> ComoDocumentosJson(this PropertyBuilder<List<DocumentoVigencia>> propiedad) =>
        propiedad
            .HasColumnType("jsonb")
            .HasConversion(
                v => ColumnasJson.Serializar(v.Select(DocumentoJson.De).ToList()),
                s => ColumnasJson.Deserializar<List<DocumentoJson>>(s).Select(d => d.A()).ToList(),
                new ValueComparer<List<DocumentoVigencia>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, d) => HashCode.Combine(h, d)),
                    v => v.ToList()));

    public static PropertyBuilder<List<Turno>> ComoTurnosJson(this PropertyBuilder<List<Turno>> propiedad) =>
        propiedad
            .HasColumnType("jsonb")
            .HasConversion(
                v => ColumnasJson.Serializar(v.Select(t => new TurnoJson(t.Dia, t.Inicio, t.Fin)).ToList()),
                s => ColumnasJson.Deserializar<List<TurnoJson>>(s).Select(t => Turno.Crear(t.Dia, t.Inicio, t.Fin)).ToList(),
                new ValueComparer<List<Turno>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t)),
                    v => v.ToList()));

    private sealed record DocumentoJson(TipoDocumento Tipo, string Numero, DateOnly VenceEl)
    {
        public static DocumentoJson De(DocumentoVigencia d) => new(d.Tipo, d.Numero, d.VenceEl);

        public DocumentoVigencia A() => DocumentoVigencia.Crear(Tipo, Numero, VenceEl);
    }

    private sealed record TurnoJson(DayOfWeek Dia, TimeOnly Inicio, TimeOnly Fin);
}
