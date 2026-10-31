using System.Globalization;
using FlowRide.Comunicacion.Aplicacion.Puertos;
using FlowRide.Identidad.Aplicacion.Puertos;
using FlowRide.Nucleo.Persistencia;
using FlowRide.Nucleo.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class CargaItinerariosTests(FabricaApi api)
{
    private const string TelefonoPasajero = "+573000000101";

    private static string Fecha(int dias) => DateTime.UtcNow.AddDays(dias).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Fact]
    public async Task LaCargaMensualCreaUnaSolicitudPorViajeYNoDuplicaAlRepetirse()
    {
        using var alcance = api.Services.CreateScope();
        var servicios = alcance.ServiceProvider;
        var identidad = servicios.GetRequiredService<IConsultaIdentidad>();
        var usuario = (await identidad.ObtenerUsuarioPorTelefonoAsync(TelefonoPasajero))!;
        var pasajero = (await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id))!;
        pasajero.DefinirDomicilio("Calle 1 # 2-3, Bogotá");
        await servicios.GetRequiredService<IUnitOfWork>().GuardarCambiosAsync();
        servicios.GetRequiredService<IContextoActorEstablecible>().Establecer(usuario.Id, usuario.Nombre, usuario.Rol);

        var carga = servicios.GetRequiredService<IServicioCargaItinerarios>();
        var archivo = string.Join(
            "\n",
            "codigo_servicio,fecha,hora_salida,hora_llegada,destino",
            $"AV100,{Fecha(10)},04:30,05:30,Aeropuerto El Dorado",
            $"AV101,{Fecha(11)},05:00,06:00,Aeropuerto El Dorado",
            $"AV102,{Fecha(-2)},05:00,06:00,Aeropuerto El Dorado");

        var lectura = carga.Previsualizar(archivo);
        Assert.Empty(lectura.Errores);

        var resultado = await carga.ConfirmarAsync(pasajero.Id, lectura.Filas);

        Assert.Equal(2, resultado.Creados.Count);
        Assert.Equal("La hora de recogida ya pasó.", Assert.Single(resultado.Omitidos).Mensaje);

        var cliente = await api.ClienteComoAsync("PAS-001");
        var solicitud = await FabricaApi.LeerAsync(await cliente.GetAsync($"/api/v1/solicitudes/{resultado.Creados[0].SolicitudId}"));
        Assert.Equal("Pendiente", solicitud.GetProperty("estado").GetString());
        Assert.Equal("Calle 1 # 2-3, Bogotá", solicitud.GetProperty("origen").GetString());
        Assert.Equal("Aeropuerto El Dorado", solicitud.GetProperty("destino").GetString());

        var repetida = await carga.ConfirmarAsync(pasajero.Id, carga.Previsualizar(archivo).Filas);

        Assert.Empty(repetida.Creados);
        Assert.Contains(repetida.Omitidos, o => o.Mensaje == "Este viaje ya estaba cargado.");
    }

    [Fact]
    public async Task SinDomicilioNoSePuedeCargarElItinerario()
    {
        using var alcance = api.Services.CreateScope();
        var servicios = alcance.ServiceProvider;
        var identidad = servicios.GetRequiredService<IConsultaIdentidad>();
        var usuario = (await identidad.ObtenerUsuarioPorTelefonoAsync("+573000000102"))!;
        var pasajero = (await identidad.ObtenerPasajeroPorUsuarioAsync(usuario.Id))!;
        var carga = servicios.GetRequiredService<IServicioCargaItinerarios>();

        var error = await Assert.ThrowsAsync<FlowRide.Nucleo.Excepciones.DomainException>(() => carga.ConfirmarAsync(pasajero.Id, []));

        Assert.Contains("domicilio", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
