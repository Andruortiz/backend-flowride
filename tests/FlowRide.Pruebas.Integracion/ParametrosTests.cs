using System.Net.Http.Json;
using FlowRide.Plataforma.Aplicacion.Puertos;
using FlowRide.Plataforma.Dominio;
using Microsoft.Extensions.DependencyInjection;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class ParametrosTests(FabricaApi api)
{
    private const string Clave = CatalogoParametros.DiasAvisoDocumentos;

    [Fact]
    public async Task CambiarUnParametroQuedaAuditadoYSeVeDeInmediatoAunConCache()
    {
        var administracion = await api.ClienteComoAsync("ADM-001");
        var original = (await FabricaApi.LeerAsync(await administracion.GetAsync("/api/v1/parametros")))
            .EnumerateArray().First(p => p.GetProperty("clave").GetString() == Clave).GetProperty("valor").GetInt32();
        var nuevo = original == 45 ? 46 : 45;

        using var alcance = api.Services.CreateScope();
        var parametros = alcance.ServiceProvider.GetRequiredService<IServicioParametros>();
        Assert.Equal(original, await parametros.ValorAsync(Clave));

        try
        {
            await FabricaApi.LeerAsync(await administracion.PutAsJsonAsync($"/api/v1/parametros/{Clave}", new { valor = nuevo }));

            Assert.Equal(nuevo, await parametros.ValorAsync(Clave));

            var auditoria = await FabricaApi.LeerAsync(await administracion.GetAsync("/api/v1/auditoria?accion=parametro.actualizar"));
            Assert.Contains(auditoria.EnumerateArray(), a => a.GetProperty("recurso").GetString() == $"parametro/{Clave}");
        }
        finally
        {
            await administracion.PutAsJsonAsync($"/api/v1/parametros/{Clave}", new { valor = original });
        }
    }

    [Fact]
    public async Task UnValorFueraDeRangoSeRechazaYNoCambiaElParametro()
    {
        var administracion = await api.ClienteComoAsync("ADM-001");

        var respuesta = await administracion.PutAsJsonAsync($"/api/v1/parametros/{Clave}", new { valor = 100000 });

        Assert.False(respuesta.IsSuccessStatusCode);
    }
}
