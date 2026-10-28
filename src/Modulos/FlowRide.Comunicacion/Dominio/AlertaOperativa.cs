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
using FlowRide.Solicitudes.Dominio;
using FlowRide.Asignaciones.Dominio;
using FlowRide.Viajes.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Comunicacion.Dominio;

public enum SeveridadAlerta
{
    Informativa = 1,
    Atencion = 2,
    Critica = 3,
}

/// <summary>Aviso para administración y operación logística en el Panel Web (RF-45). La clave evita duplicados.</summary>
public sealed class AlertaOperativa : Entidad
{
    private AlertaOperativa(
        Guid id,
        string tipo,
        SeveridadAlerta severidad,
        string titulo,
        string detalle,
        string referencia,
        string claveDeduplicacion,
        DateTimeOffset creadaEn)
        : base(id)
    {
        Tipo = tipo;
        Severidad = severidad;
        Titulo = titulo;
        Detalle = detalle;
        Referencia = referencia;
        ClaveDeduplicacion = claveDeduplicacion;
        CreadaEn = creadaEn;
    }

    public string Tipo { get; }

    public SeveridadAlerta Severidad { get; }

    public string Titulo { get; }

    public string Detalle { get; }

    /// <summary>Identificador del recurso al que se refiere (viaje, solicitud, conductor…).</summary>
    public string Referencia { get; }

    public string ClaveDeduplicacion { get; }

    public DateTimeOffset CreadaEn { get; }

    public DateTimeOffset? LeidaEn { get; private set; }

    public static AlertaOperativa Crear(
        string tipo,
        SeveridadAlerta severidad,
        string titulo,
        string detalle,
        string referencia,
        string claveDeduplicacion,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        if (TextHelper.IsEmptyWithTrim(tipo) || TextHelper.IsEmptyWithTrim(titulo) || TextHelper.IsEmptyWithTrim(claveDeduplicacion))
        {
            throw new DomainException("La alerta requiere tipo, título y clave de deduplicación.");
        }

        return new AlertaOperativa(
            UuidHelper.GetOrGenerate(id),
            tipo.Trim(),
            severidad,
            titulo.Trim(),
            TextHelper.GetDefaultWithTrim(detalle),
            TextHelper.GetDefaultWithTrim(referencia),
            claveDeduplicacion.Trim(),
            ahora);
    }

    public void MarcarLeida(DateTimeOffset ahora) => LeidaEn ??= ahora;
}
