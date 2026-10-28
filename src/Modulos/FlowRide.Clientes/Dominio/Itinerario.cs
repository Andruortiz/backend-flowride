using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Clientes.Dominio;

/// <summary>Servicio programado o recurrente que registra el operador logístico (RF-08). Las horas son locales del operador.</summary>
public sealed class Itinerario : Entidad
{
    private Itinerario(
        Guid id,
        string codigoServicio,
        Guid? clienteEmpresarialId,
        string origen,
        string destino,
        DateOnly fecha,
        TimeOnly horaSalida,
        TimeOnly horaLlegada,
        DateTimeOffset creadoEn)
        : base(id)
    {
        CodigoServicio = codigoServicio;
        ClienteEmpresarialId = clienteEmpresarialId;
        Origen = origen;
        Destino = destino;
        Fecha = fecha;
        HoraSalida = horaSalida;
        HoraLlegada = horaLlegada;
        CreadoEn = creadoEn;
    }

    public string CodigoServicio { get; }

    public Guid? ClienteEmpresarialId { get; }

    public string Origen { get; }

    public string Destino { get; }

    public DateOnly Fecha { get; }

    public TimeOnly HoraSalida { get; }

    public TimeOnly HoraLlegada { get; }

    public DateTimeOffset CreadoEn { get; }

    public static Itinerario Crear(
        string codigoServicio,
        Guid? clienteEmpresarialId,
        string origen,
        string destino,
        DateOnly fecha,
        TimeOnly horaSalida,
        TimeOnly horaLlegada,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        if (TextHelper.IsEmptyWithTrim(codigoServicio))
        {
            throw new DomainException("El código de servicio es obligatorio.");
        }

        if (TextHelper.IsEmptyWithTrim(origen) || TextHelper.IsEmptyWithTrim(destino))
        {
            throw new DomainException("El origen y el destino del itinerario son obligatorios.");
        }

        if (string.Equals(origen.Trim(), destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("El origen y el destino deben ser distintos.");
        }

        if (fecha == default)
        {
            throw new DomainException("La fecha del itinerario es obligatoria.");
        }

        if (horaSalida == horaLlegada)
        {
            throw new DomainException("La hora de salida y la de llegada no pueden ser iguales.");
        }

        return new Itinerario(
            UuidHelper.GetOrGenerate(id),
            codigoServicio.Trim(),
            clienteEmpresarialId,
            origen.Trim(),
            destino.Trim(),
            fecha,
            horaSalida,
            horaLlegada,
            ahora);
    }
}
