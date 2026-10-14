using System.Net.Mail;
using System.Text.RegularExpressions;
using FlowRide.Dominio.Compartido;
using FlowRide.Transversal.Excepciones;
using FlowRide.Transversal.Helpers;

namespace FlowRide.Dominio.Usuarios;

public sealed class Usuario : Entidad
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
            NormalizarTextoObligatorio(nombre, "El nombre del usuario es obligatorio."),
            NormalizarTextoObligatorio(codigoInterno, "El código interno del usuario es obligatorio."),
            rol,
            ahora)
        {
            Correo = correoNormalizado,
            Telefono = telefonoNormalizado,
        };
    }

    public void ActualizarDatos(string nombre, string? correo, string? telefono)
    {
        var nombreNormalizado = NormalizarTextoObligatorio(nombre, "El nombre del usuario es obligatorio.");
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
        var identidad = NormalizarTextoObligatorio(identidadExterna, "La identidad externa es obligatoria.");

        if (IdentidadExterna is not null && IdentidadExterna != identidad)
        {
            throw new DomainException("El usuario ya está vinculado a otra identidad externa.");
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
            throw new DomainException("El correo electrónico no es válido.");
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
            throw new DomainException("El teléfono debe estar en formato internacional, por ejemplo +573001234567.");
        }

        return limpio;
    }

    private static void ValidarContactoSegunRol(RolUsuario rol, string? correo, string? telefono)
    {
        var usaWhatsApp = rol is RolUsuario.Pasajero or RolUsuario.Conductor;

        if (usaWhatsApp && telefono is null)
        {
            throw new DomainException("Pasajeros y conductores requieren teléfono: se identifican por WhatsApp.");
        }

        if (!usaWhatsApp && correo is null)
        {
            throw new DomainException("Administradores y operadores logísticos requieren correo: acceden al Panel Web.");
        }
    }

}