using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;

namespace FlowRide.Plataforma.Dominio;

/// <summary>Valor vigente de un parámetro del operador, con versión y quién lo cambió.</summary>
public sealed class ParametroOperador : Entidad
{
    private ParametroOperador(Guid id, string clave, int valor, DateTimeOffset modificadoEn)
        : base(id)
    {
        Clave = clave;
        Valor = valor;
        Version = 1;
        ModificadoEn = modificadoEn;
    }

    public string Clave { get; }

    public int Valor { get; private set; }

    public int Version { get; private set; }

    public Guid? ModificadoPor { get; private set; }

    public DateTimeOffset ModificadoEn { get; private set; }

    public static ParametroOperador Crear(string clave, int valor, DateTimeOffset ahora, Guid? id = null)
    {
        var definicion = CatalogoParametros.Obtener(clave);
        Validar(definicion, valor);

        return new ParametroOperador(UuidHelper.GetOrGenerate(id), clave, valor, ahora);
    }

    public void Actualizar(int valor, Guid? usuarioId, DateTimeOffset ahora)
    {
        Validar(CatalogoParametros.Obtener(Clave), valor);

        Valor = valor;
        Version++;
        ModificadoPor = usuarioId;
        ModificadoEn = ahora;
    }

    private static void Validar(DefinicionParametro definicion, int valor)
    {
        if (valor < definicion.Minimo || valor > definicion.Maximo)
        {
            throw new DomainException(
                $"{definicion.Descripcion}: el valor debe estar entre {definicion.Minimo} y {definicion.Maximo} {definicion.Unidad}.");
        }
    }
}
