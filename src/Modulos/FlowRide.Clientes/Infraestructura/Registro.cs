using FlowRide.Clientes.Aplicacion;
using FlowRide.Clientes.Aplicacion.Puertos;
using FlowRide.Clientes.Infraestructura.Repositorios;
using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Clientes.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloClientes(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IClienteEmpresarialRepository, ClienteEmpresarialRepository>();
        servicios.AddScoped<IConsultaClientes, ConsultaClientes>();
        servicios.AddScoped<IItinerarioRepository, ItinerarioRepository>();
        servicios.AddScoped<IServicioClientes, ServicioClientes>();

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
