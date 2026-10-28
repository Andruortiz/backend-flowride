using System.Text.RegularExpressions;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Flota.Dominio;

public sealed class Vehiculo : Entidad
{
    public static readonly IReadOnlyList<TipoDocumento> DocumentosRequeridos =
    [
        TipoDocumento.Soat,
        TipoDocumento.RevisionTecnicoMecanica,
        TipoDocumento.TarjetaOperacion,
    ];

    private static readonly Regex FormatoPlaca =
          new(@"^[A-Z]{3}\d{2}[A-Z0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly List<DocumentoVigencia> _documentos = [];

    private Vehiculo(Guid id, string placa, int capacidad)
        : base(id)
    {
        Placa = placa;
        Capacidad = capacidad;
        Habilitado = true;
    }

    public string Placa { get; }

    public int Capacidad { get; private set; }

    public bool Habilitado { get; private set; }

    public IReadOnlyCollection<DocumentoVigencia> Documentos => _documentos;

    public static Vehiculo Crear(string placa, int capacidad, Guid? id = null)
    {
        ValidarCapacidad(capacidad);

        return new Vehiculo(UuidHelper.GetOrGenerate(id), NormalizarPlaca(placa), capacidad);
    }

    public void CambiarCapacidad(int capacidad)
    {
        ValidarCapacidad(capacidad);

        Capacidad = capacidad;
    }

    public void RegistrarDocumento(DocumentoVigencia documento)
    {
        ArgumentNullException.ThrowIfNull(documento);

        if (!documento.Tipo.EsDeVehiculo())
        {
            throw new DomainException("Este documento no corresponde a un vehículo.");
        }

        _documentos.RemoveAll(d => d.Tipo == documento.Tipo);
        _documentos.Add(documento);
    }

    public DocumentoVigencia? DocumentoDe(TipoDocumento tipo) =>
        _documentos.Find(d => d.Tipo == tipo);

    public void Habilitar() => Habilitado = true;

    public void Deshabilitar() => Habilitado = false;

    public IReadOnlyList<TipoDocumento> DocumentosNoVigentesEn(DateOnly fecha) =>
        DocumentosRequeridos
            .Where(tipo => !_documentos.Exists(d => d.Tipo == tipo && d.EstaVigenteEn(fecha)))
            .ToList();

    public bool EsAsignableEn(DateOnly fecha) =>
        Habilitado && DocumentosNoVigentesEn(fecha).Count == 0;

    private static string NormalizarPlaca(string placa)
    {
        if (TextHelper.IsEmptyWithTrim(placa))
        {
            throw new DomainException("La placa del vehículo es obligatoria.");
        }

        var limpia = placa
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        if (!FormatoPlaca.IsMatch(limpia))
        {
            throw new DomainException("La placa no tiene un formato válido, por ejemplo ABC123.");
        }

        return limpia;
    }

    private static void ValidarCapacidad(int capacidad)
    {
        if (capacidad < 1)
        {
            throw new DomainException("La capacidad del vehículo debe ser de al menos 1 pasajero.");
        }
    }
}
