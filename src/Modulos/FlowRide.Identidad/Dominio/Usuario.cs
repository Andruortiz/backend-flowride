using System.Net.Mail;
using System.Text.RegularExpressions;
using FlowRide.Nucleo.Excepciones;
using FlowRide.Nucleo.Helpers;
using FlowRide.Nucleo.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Eventos;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Contratos;
using FlowRide.Clientes.Dominio;
using FlowRide.Plataforma.Dominio;

namespace FlowRide.Identidad.Dominio;

public sealed class Usuario : RaizAgregado
{
    private static readonly Regex FormatoTelefono =
        new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Usuario(Guid id, string nombre, string codigoInterno, RolUsuario rol, DateTimeOffset creadoEn)
        : base(id)
    {
        Nombre = nombre;
        CodigoInterno = codigoInterno;
        Rol = rol;
        CreadoEn = creadoEn;
        Activo = true;
    }

    public string Nombre { get; private set; }

    public string? Correo { get; private set; }

    public string? Telefono { get; private set; }

    public string CodigoInterno { get; }

    public RolUsuario Rol { get; private set; }

    public string? IdentidadExterna { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset CreadoEn { get; }

    public DateTimeOffset? DesactivadoEn { get; private set; }

    public static Usuario Crear(
        string nombre,
        string? correo,
        string? telefono,
        string codigoInterno,
        RolUsuario rol,
        DateTimeOffset ahora,
        Guid? id = null)
    {
        var correoNormalizado = NormalizarCorreo(correo);
        var telefonoNormalizado = NormalizarTelefono(telefono);

        ValidarContactoSegunRol(rol, correoNormalizado, telefonoNormalizado);

        return new Usuario(
            UuidHelper.GetOrGenerate(id),
            NormalizarTextoObligatorio(nombre, "error.el_nombre_del_usuario_es_obligatorio"),
            NormalizarTextoObligatorio(codigoInterno, "error.el_codigo_interno_del_usuario_es_obligatorio"),
            rol,
            ahora)
        {
            Correo = correoNormalizado,
            Telefono = telefonoNormalizado,
        };
    }

    public void ActualizarDatos(string nombre, string? correo, string? telefono)
    {
        var nombreNormalizado = NormalizarTextoObligatorio(nombre, "error.el_nombre_del_usuario_es_obligatorio");
        var correoNormalizado = NormalizarCorreo(correo);
        var telefonoNormalizado = NormalizarTelefono(telefono);

        ValidarContactoSegunRol(Rol, correoNormalizado, telefonoNormalizado);

        Nombre = nombreNormalizado;
        Correo = correoNormalizado;
        Telefono = telefonoNormalizado;
    }

    public void CambiarRol(RolUsuario nuevoRol)
    {
        ValidarContactoSegunRol(nuevoRol, Correo, Telefono);

        Rol = nuevoRol;
    }

    public void VincularIdentidadExterna(string identidadExterna)
    {
        var identidad = NormalizarTextoObligatorio(identidadExterna, "error.la_identidad_externa_es_obligatoria");

        if (IdentidadExterna is not null && IdentidadExterna != identidad)
        {
            throw new DomainException("error.el_usuario_ya_esta_vinculado_a_otra");
        }

        IdentidadExterna = identidad;
    }

    public void Desactivar(DateTimeOffset ahora)
    {
        if (!Activo)
        {
            return;
        }

        Activo = false;
        DesactivadoEn = ahora;
        Registrar(new UsuarioDesactivado(Id, ahora));
    }

    public void Activar()
    {
        if (Activo)
        {
            return;
        }

        Activo = true;
        DesactivadoEn = null;
    }

    private static string NormalizarTextoObligatorio(string valor, string mensaje)
    {
        if (TextHelper.IsEmptyWithTrim(valor))
        {
            throw new DomainException(mensaje);
        }

        return valor.Trim();
    }

    private static string? NormalizarCorreo(string? correo)
    {
        if (TextHelper.IsEmptyWithTrim(correo))
        {
            return null;
        }

        var limpio = correo.Trim().ToLowerInvariant();

        if (!MailAddress.TryCreate(limpio, out var direccion) || direccion.Address != limpio)
        {
            throw new DomainException("error.el_correo_electronico_no_es_valido");
        }

        return limpio;
    }

    private static string? NormalizarTelefono(string? telefono)
    {
        if (TextHelper.IsEmptyWithTrim(telefono))
        {
            return null;
        }

        var limpio = telefono
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (!FormatoTelefono.IsMatch(limpio))
        {
            throw new DomainException("error.el_telefono_debe_estar_en_formato_internacional");
        }

        return limpio;
    }

    private static void ValidarContactoSegunRol(RolUsuario rol, string? correo, string? telefono)
    {
        var usaWhatsApp = rol is RolUsuario.Pasajero or RolUsuario.Conductor;

        if (usaWhatsApp && telefono is null)
        {
            throw new DomainException("error.pasajeros_y_conductores_requieren_telefono_se_identifican");
        }

        if (!usaWhatsApp && correo is null)
        {
            throw new DomainException("error.administradores_y_operadores_logisticos_requieren_correo_acceden");
        }
    }

}
