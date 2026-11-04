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

public sealed record DocumentoVigencia
{
    public const int DiasAvisoPorDefecto = 30;

    private DocumentoVigencia(TipoDocumento tipo, string numero, DateOnly venceEl)
    {
        Tipo = tipo;
        Numero = numero;
        VenceEl = venceEl;
    }

    public TipoDocumento Tipo { get; }

    public string Numero { get; }

    public DateOnly VenceEl { get; }

    public static DocumentoVigencia Crear(TipoDocumento tipo, string numero, DateOnly venceEl)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new DomainException("error.el_tipo_de_documento_no_es_valido");
        }

        if (TextHelper.IsEmptyWithTrim(numero))
        {
            throw new DomainException("error.el_numero_del_documento_es_obligatorio");
        }

        if (venceEl == default)
        {
            throw new DomainException("error.la_fecha_de_vencimiento_del_documento_es");
        }

        return new DocumentoVigencia(tipo, numero.Trim(), venceEl);
    }

    public bool EstaVigenteEn(DateOnly hoy) => hoy <= VenceEl;

    public int DiasParaVencer(DateOnly hoy) => VenceEl.DayNumber - hoy.DayNumber;

    public EstadoVigencia EstadoEn(DateOnly hoy, int diasAviso = DiasAvisoPorDefecto)
    {
        if (!EstaVigenteEn(hoy))
        {
            return EstadoVigencia.Vencido;
        }

        return DiasParaVencer(hoy) <= diasAviso ? EstadoVigencia.PorVencer : EstadoVigencia.Vigente;
    }

    public DocumentoVigencia Renovar(string nuevoNumero, DateOnly nuevoVencimiento)
    {
        if (nuevoVencimiento <= VenceEl)
        {
            throw new DomainException("error.la_renovacion_debe_vencer_despues_de_la");
        }

        return Crear(Tipo, nuevoNumero, nuevoVencimiento);
    }
}
