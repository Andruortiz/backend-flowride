using FlowRide.Clientes.Dominio;
using FlowRide.Flota.Dominio;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Persistencia;
using FlowRide.Plataforma.Dominio;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Inicializador.Configuracion;

/// <summary>Datos de prueba para desarrollo local. Solo se ejecuta en Development y solo si la base está vacía.</summary>
internal static class SembradoDesarrollo
{
    public static async Task EjecutarAsync(IServiceProvider servicios, CancellationToken ct = default)
    {
        await using var alcance = servicios.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<FlowRideDbContext>();
        var tenant = alcance.ServiceProvider.GetRequiredService<IContextoTenant>();
        var reloj = alcance.ServiceProvider.GetRequiredService<TimeProvider>();

        if (await db.Set<Operador>().AnyAsync(ct))
        {
            return;
        }

        var ahora = reloj.GetUtcNow();
        db.Set<Operador>().Add(Operador.Crear("Operador de Desarrollo", "900000000-1", "operaciones@desarrollo.local", "America/Bogota", ahora, tenant.TenantId));

        var cliente = ClienteEmpresarial.Crear("Empresa Demo S.A.S.", "900111222-3");
        db.Set<ClienteEmpresarial>().Add(cliente);

        db.Set<Usuario>().Add(Usuario.Crear("Ana Administradora", "admin@desarrollo.local", null, "ADM-001", RolUsuario.AdministradorOperador, ahora));
        db.Set<Usuario>().Add(Usuario.Crear("Omar Operador", "operacion@desarrollo.local", null, "OPL-001", RolUsuario.OperadorLogistico, ahora));

        for (var i = 1; i <= 2; i++)
        {
            var usuario = Usuario.Crear($"Pasajero {i}", null, $"+57300000010{i}", $"PAS-00{i}", RolUsuario.Pasajero, ahora);
            db.Set<Usuario>().Add(usuario);
            db.Set<Pasajero>().Add(Pasajero.Crear(usuario, cliente.Id));
        }

        var hoy = DateOnly.FromDateTime(ahora.UtcDateTime);

        for (var i = 1; i <= 3; i++)
        {
            var usuario = Usuario.Crear($"Conductor {i}", null, $"+57300000020{i}", $"CON-00{i}", RolUsuario.Conductor, ahora);
            var vehiculo = Vehiculo.Crear($"DEV{i}0{i}", 4);

            foreach (var tipo in Vehiculo.DocumentosRequeridos)
            {
                vehiculo.RegistrarDocumento(DocumentoVigencia.Crear(tipo, $"DOC-{i}-{(int)tipo}", hoy.AddYears(1)));
            }

            var conductor = Conductor.Crear(usuario, DocumentoVigencia.Crear(TipoDocumento.LicenciaConduccion, $"LIC-00{i}", hoy.AddYears(2)));
            conductor.AsignarVehiculo(vehiculo);
            conductor.DefinirTurnos(Enum.GetValues<DayOfWeek>().Select(d => Turno.Crear(d, new TimeOnly(0, 0), new TimeOnly(23, 59))));

            db.Set<Usuario>().Add(usuario);
            db.Set<Vehiculo>().Add(vehiculo);
            db.Set<Conductor>().Add(conductor);
        }

        await db.SaveChangesAsync(ct);
    }
}
