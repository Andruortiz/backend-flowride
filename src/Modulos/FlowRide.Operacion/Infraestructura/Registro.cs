using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Operacion.Aplicacion;
using FlowRide.Operacion.Aplicacion.Puertos;
using FlowRide.Operacion.Infraestructura.Repositorios;
using FlowRide.Persistencia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Operacion.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloOperacion(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IReportesQuery, ReportesQuery>();
        servicios.AddScoped<IServicioPlanificacion, ServicioPlanificacion>();
        servicios.AddScoped<IServicioReportes, ServicioReportes>();
        servicios.Configure<OpcionesPlanificador>(configuracion.GetSection("Planificador"));
        if (configuracion.GetValue("Planificador:Habilitado", true))
        {
            servicios.AddHostedService<PlanificadorBackground>();
        }

        return servicios;
    }
}
