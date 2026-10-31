using FlowRide.Contratos;
using FlowRide.Nucleo.Eventos;
using FlowRide.Persistencia;
using FlowRide.Plataforma.Aplicacion;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Plataforma.Infraestructura.Repositorios;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Plataforma.Infraestructura;

public static class Registro
{
    public static IServiceCollection AgregarModuloPlataforma(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IConfiguradorModelo, ConfiguracionModelo>();
        servicios.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        servicios.AddScoped<IOperadorRepository, OperadorRepository>();
        servicios.AddScoped<IParametroRepository, ParametroRepository>();
        servicios.AddScoped<IServicioAuditoria, ServicioAuditoria>();
        servicios.AddScoped<IServicioOperador, ServicioOperador>();
        servicios.AddScoped<IServicioParametros, ServicioParametros>();

        return servicios;
    }

    public static IConfiguradorModelo CrearConfiguradorModelo() => new ConfiguracionModelo();
}
