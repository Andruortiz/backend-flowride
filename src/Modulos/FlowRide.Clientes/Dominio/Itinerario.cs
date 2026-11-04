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
        DateTimeOffset creadoEn,
        Guid? pasajeroId)
        : base(id)
    {
        PasajeroId = pasajeroId;
        CodigoServicio = codigoServicio;
        ClienteEmpresarialId = clienteEmpresarialId;
        Origen = origen;
        Destino = destino;
        Fecha = fecha;
        HoraSalida = horaSalida;
        HoraLlegada = horaLlegada;
        CreadoEn = creadoEn;
    }

    public Guid? PasajeroId { get; }

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
        Guid? id = null,
        Guid? pasajeroId = null)
    {
        if (TextHelper.IsEmptyWithTrim(codigoServicio))
        {
            throw new DomainException("error.el_codigo_de_servicio_es_obligatorio");
        }

        if (TextHelper.IsEmptyWithTrim(origen) || TextHelper.IsEmptyWithTrim(destino))
        {
            throw new DomainException("error.el_origen_y_el_destino_del_itinerario");
        }

        if (string.Equals(origen.Trim(), destino.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("error.el_origen_y_el_destino_deben_ser");
        }

        if (fecha == default)
        {
            throw new DomainException("error.la_fecha_del_itinerario_es_obligatoria");
        }

        if (horaSalida == horaLlegada)
        {
            throw new DomainException("error.la_hora_de_salida_y_la_de");
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
            ahora,
            pasajeroId);
    }
}
