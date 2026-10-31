using FlowRide.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowRide.Inicializador.Configuracion;

internal sealed class FabricaDbContextDiseno : IDesignTimeDbContextFactory<FlowRideDbContext>
{
    public FlowRideDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("FLOWRIDE_DB") ?? "Host=localhost;Database=flowride;Username=postgres";
        var opciones = new DbContextOptionsBuilder<FlowRideDbContext>()
            .UseNpgsql(conexion, o => o.MigrationsAssembly(typeof(FabricaDbContextDiseno).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        IConfiguradorModelo[] configuradores =
        [
            FlowRide.Plataforma.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Clientes.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Identidad.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Flota.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Solicitudes.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Viajes.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Asignaciones.Infraestructura.Registro.CrearConfiguradorModelo(),
            FlowRide.Comunicacion.Infraestructura.Registro.CrearConfiguradorModelo(),
        ];

        return new FlowRideDbContext(opciones, TimeProvider.System, configuradores);
    }
}
