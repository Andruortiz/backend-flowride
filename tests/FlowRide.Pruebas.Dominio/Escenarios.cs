
using FlowRide.Asignaciones.Dominio;
using FlowRide.Clientes.Dominio;
using FlowRide.Contratos;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Solicitudes.Dominio;

namespace FlowRide.Pruebas.Dominio;

internal static class Escenarios
{
    public static readonly DateTimeOffset Ahora = new(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Fecha = new(2026, 10, 14);
    public static readonly TimeOnly Hora = new(8, 30);
    public static readonly DateOnly VenceLejos = new(2027, 12, 31);

    public static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    public static Usuario UsuarioPasajero(string codigo = "P-001") =>
        Usuario.Crear("Ana Pérez", null, "+573001234567", codigo, RolUsuario.Pasajero, Ahora);

    public static Pasajero NuevoPasajero(Guid? clienteId = null) =>
        Pasajero.Crear(UsuarioPasajero(), clienteId ?? Id(900));

    public static Vehiculo VehiculoAlDia(string placa = "ABC123", int capacidad = 1, DateOnly? vence = null)
    {
        var fecha = vence ?? VenceLejos;
        var vehiculo = Vehiculo.Crear(placa, capacidad);
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.Soat, "S-1", fecha));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.RevisionTecnicoMecanica, "R-1", fecha));
        vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(TipoDocumento.TarjetaOperacion, "T-1", fecha));

        return vehiculo;
    }

    public static Conductor ConductorListo(int n, DateOnly? venceLicencia = null)
    {
        var usuario = Usuario.Crear($"Conductor {n}", null, $"+57300{n:D7}", $"C-{n:D3}", RolUsuario.Conductor, Ahora);
        var conductor = Conductor.Crear(
            usuario,
            DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, $"L-{n}", venceLicencia ?? VenceLejos),
            Id(n));
        conductor.DefinirTurnos([Turno.Crear(Fecha.DayOfWeek, new TimeOnly(5, 0), new TimeOnly(13, 0))]);

        return conductor;
    }

    public static CandidatoAsignacion Candidato(int n, int carga = 0, int recientes = 0, Vehiculo? vehiculo = null) =>
        new(ConductorListo(n), vehiculo ?? VehiculoAlDia($"AB{(char)(65 + (n % 26))}{n % 100:D2}1"), carga, recientes);

    public static SolicitudTransporte Solicitud(bool urgente = false, DateTimeOffset? cuando = null, bool personal = false) =>
        SolicitudTransporte.Registrar(
            NuevoPasajero(), "Aeropuerto", "Hotel Poblado", cuando ?? Ahora.AddHours(6), urgente, personal, Ahora);
}
