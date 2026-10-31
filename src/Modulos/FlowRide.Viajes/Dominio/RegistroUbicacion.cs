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

public enum CalidadUbicacion
{
    Continua = 1,
    Puntual = 2,
}

public sealed class RegistroUbicacion : Entidad
{
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(2);

    private RegistroUbicacion(
        Guid id,
        Guid vehiculoId,
        Guid? conductorId,
        Guid? viajeId,
        DateTimeOffset timestampOrigen,
        DateTimeOffset timestampRecepcion,
        double latitud,
        double longitud,
        CalidadUbicacion calidad,
        string fuente)
        : base(id)
    {
        VehiculoId = vehiculoId;
        ConductorId = conductorId;
        ViajeId = viajeId;
        TimestampOrigen = timestampOrigen;
        TimestampRecepcion = timestampRecepcion;
        Latitud = latitud;
        Longitud = longitud;
        Calidad = calidad;
        Fuente = fuente;
    }

    public Guid VehiculoId { get; }

    public Guid? ConductorId { get; }

    public Guid? ViajeId { get; }

    public DateTimeOffset TimestampOrigen { get; }

    public DateTimeOffset TimestampRecepcion { get; }

    public double Latitud { get; }

    public double Longitud { get; }

    public CalidadUbicacion Calidad { get; }

    public string Fuente { get; }

    public TimeSpan RetrasoIngesta => TimestampRecepcion - TimestampOrigen;

    public Coordenada Posicion => Coordenada.Crear(Latitud, Longitud);

    public static RegistroUbicacion Crear(
        Guid vehiculoId,
        Guid? conductorId,
        Guid? viajeId,
        DateTimeOffset timestampOrigen,
        DateTimeOffset timestampRecepcion,
        double latitud,
        double longitud,
        CalidadUbicacion calidad,
        string fuente,
        int ventanaMinutos,
        Guid? id = null)
    {
        if (vehiculoId == Guid.Empty)
        {
            throw new DomainException("El vehículo es obligatorio.");
        }

        if (TextHelper.IsEmptyWithTrim(fuente))
        {
            throw new DomainException("La fuente de la ubicación es obligatoria.");
        }

        _ = Coordenada.Crear(latitud, longitud);

        if (timestampOrigen - timestampRecepcion > ToleranciaFuturo)
        {
            throw new DomainException("El instante de origen está en el futuro.");
        }

        if (timestampRecepcion - timestampOrigen > TimeSpan.FromMinutes(ventanaMinutos))
        {
            throw new DomainException($"El evento es más antiguo que la ventana permitida de {ventanaMinutos} minutos.");
        }

        return new RegistroUbicacion(
            UuidHelper.GetOrGenerate(id),
            vehiculoId,
            conductorId,
            viajeId,
            timestampOrigen,
            timestampRecepcion,
            latitud,
            longitud,
            calidad,
            fuente.Trim().ToLowerInvariant());
    }
}
