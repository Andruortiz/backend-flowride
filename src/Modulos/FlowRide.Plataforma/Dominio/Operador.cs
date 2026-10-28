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

/// <summary>El operador de transporte al que sirve esta instancia (un Silo por operador).</summary>
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

    /// <summary>Identificador IANA (por ejemplo America/Bogota). Define el "día" del servicio y los turnos.</summary>
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
            Obligatorio(razonSocial, "La razón social del operador es obligatoria."),
            Obligatorio(nit, "El NIT del operador es obligatorio."),
            NormalizarCorreo(correoContacto),
            ValidarZona(zonaHoraria),
            ahora);

    public void ActualizarDatos(string razonSocial, string nit, string correoContacto, string zonaHoraria)
    {
        var razon = Obligatorio(razonSocial, "La razón social del operador es obligatoria.");
        var nitNormalizado = Obligatorio(nit, "El NIT del operador es obligatorio.");
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
        var limpio = Obligatorio(correo, "El correo de contacto es obligatorio.").ToLowerInvariant();

        return MailAddress.TryCreate(limpio, out var direccion) && direccion.Address == limpio
            ? limpio
            : throw new DomainException("El correo de contacto no es válido.");
    }

    private static string ValidarZona(string zona)
    {
        var limpia = Obligatorio(zona, "La zona horaria del operador es obligatoria.");

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(limpia);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new DomainException($"La zona horaria '{limpia}' no existe.");
        }

        return limpia;
    }
}
