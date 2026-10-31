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

public static class EstimadorEta
{
    private const double VelocidadMinimaKmh = 5;
    private const double VelocidadMaximaKmh = 120;

    public static DateTimeOffset? EstimarLlegada(IReadOnlyList<RegistroUbicacion> recientes, Coordenada destino)
    {
        ArgumentNullException.ThrowIfNull(recientes);
        ArgumentNullException.ThrowIfNull(destino);

        if (recientes.Count < 2)
        {
            return null;
        }

        var anterior = recientes[^2];
        var ultima = recientes[^1];
        var horas = (ultima.TimestampOrigen - anterior.TimestampOrigen).TotalHours;

        if (horas <= 0)
        {
            return null;
        }

        var velocidad = Math.Clamp(anterior.Posicion.DistanciaKm(ultima.Posicion) / horas, VelocidadMinimaKmh, VelocidadMaximaKmh);
        var restanteKm = ultima.Posicion.DistanciaKm(destino);

        return ultima.TimestampOrigen.AddHours(restanteKm / velocidad);
    }
}
