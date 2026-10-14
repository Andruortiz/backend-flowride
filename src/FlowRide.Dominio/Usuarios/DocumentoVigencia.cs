using FlowRide.Transversal.Excepciones;
using FlowRide.Transversal.Helpers;

namespace FlowRide.Dominio.Usuarios;

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
            throw new DomainException("El tipo de documento no es válido.");
        }

        if (TextHelper.IsEmptyWithTrim(numero))
        {
            throw new DomainException("El número del documento es obligatorio.");
        }

        if (venceEl == default)
        {
            throw new DomainException("La fecha de vencimiento del documento es obligatoria.");
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
            throw new DomainException("La renovación debe vencer después de la fecha de vencimiento actual.");
        }

        return Crear(Tipo, nuevoNumero, nuevoVencimiento);
    }
}