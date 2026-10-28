using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Identidad.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Viajes.Dominio;

/// <summary>Punto geográfico válido (WGS84, grados decimales).</summary>
public sealed record Coordenada
{
    private const double RadioTierraKm = 6371.0088;

    private Coordenada(double latitud, double longitud)
    {
        Latitud = latitud;
        Longitud = longitud;
    }

    public double Latitud { get; }

    public double Longitud { get; }

    public static Coordenada Crear(double latitud, double longitud)
    {
        if (double.IsNaN(latitud) || latitud is < -90 or > 90)
        {
            throw new DomainException("La latitud debe estar entre -90 y 90.");
        }

        if (double.IsNaN(longitud) || longitud is < -180 or > 180)
        {
            throw new DomainException("La longitud debe estar entre -180 y 180.");
        }

        return new Coordenada(latitud, longitud);
    }

    /// <summary>Distancia por el círculo máximo (fórmula de Haversine).</summary>
    public double DistanciaKm(Coordenada otra)
    {
        ArgumentNullException.ThrowIfNull(otra);

        var dLat = ARadianes(otra.Latitud - Latitud);
        var dLon = ARadianes(otra.Longitud - Longitud);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(ARadianes(Latitud)) * Math.Cos(ARadianes(otra.Latitud)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        return RadioTierraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ARadianes(double grados) => grados * Math.PI / 180;
}
