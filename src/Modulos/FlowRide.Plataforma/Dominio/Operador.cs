using System.Net.Mail;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;

namespace FlowRide.Plataforma.Dominio;

public sealed class Operador : Entidad
{
    private Operador(Guid id, string razonSocial, string nit, string correoContacto, string zonaHoraria, DateTimeOffset creadoEn)
        : base(id)
    {
        RazonSocial = razonSocial;
        Nit = nit;
        CorreoContacto = correoContacto;
        ZonaHoraria = zonaHoraria;
        CreadoEn = creadoEn;
        Activo = true;
    }

    public string RazonSocial { get; private set; }

    public string Nit { get; private set; }

    public string CorreoContacto { get; private set; }

    public string ZonaHoraria { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset CreadoEn { get; }

    public static Operador Crear(
        string razonSocial,
        string nit,
        string correoContacto,
        string zonaHoraria,
        DateTimeOffset ahora,
        Guid? id = null) =>
        new(
            UuidHelper.GetOrGenerate(id),
            Obligatorio(razonSocial, "error.la_razon_social_del_operador_es_obligatoria"),
            Obligatorio(nit, "error.el_nit_del_operador_es_obligatorio"),
            NormalizarCorreo(correoContacto),
            ValidarZona(zonaHoraria),
            ahora);

    public void ActualizarDatos(string razonSocial, string nit, string correoContacto, string zonaHoraria)
    {
        var razon = Obligatorio(razonSocial, "error.la_razon_social_del_operador_es_obligatoria");
        var nitNormalizado = Obligatorio(nit, "error.el_nit_del_operador_es_obligatorio");
        var correo = NormalizarCorreo(correoContacto);
        var zona = ValidarZona(zonaHoraria);

        RazonSocial = razon;
        Nit = nitNormalizado;
        CorreoContacto = correo;
        ZonaHoraria = zona;
    }

    public TimeZoneInfo ObtenerZona() => TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria);

    private static string Obligatorio(string valor, string mensaje) =>
        TextHelper.IsEmptyWithTrim(valor) ? throw new DomainException(mensaje) : valor.Trim();

    private static string NormalizarCorreo(string correo)
    {
        var limpio = Obligatorio(correo, "error.el_correo_de_contacto_es_obligatorio").ToLowerInvariant();

        return MailAddress.TryCreate(limpio, out var direccion) && direccion.Address == limpio
            ? limpio
            : throw new DomainException("error.el_correo_de_contacto_no_es_valido");
    }

    private static string ValidarZona(string zona)
    {
        var limpia = Obligatorio(zona, "error.la_zona_horaria_del_operador_es_obligatoria");

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(limpia);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new DomainException("error.la_zona_horaria_no_existe", ("limpia", limpia));
        }

        return limpia;
    }
}
