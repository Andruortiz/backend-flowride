using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace FlowRide.Pruebas.Integracion;

/// <summary>
/// Una sola instancia de la API y de la base para todas las clases de prueba. No puede haber dos a la vez:
/// la configuración viaja por variables de entorno, que son globales del proceso.
/// </summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<FabricaApi>
{
    public const string Nombre = "api";
}

/// <summary>API completa sobre un PostgreSQL real y desechable.</summary>
public sealed class FabricaApi : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SecretoWebhook = "secreto-solo-para-pruebas";

    private readonly PostgreSqlContainer _base = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _base.StartAsync();

        // Variables de entorno: Program las lee al construir el host, antes de cualquier configuración de la fábrica.
        Environment.SetEnvironmentVariable("ConnectionStrings__FlowRide", _base.GetConnectionString());
        Environment.SetEnvironmentVariable("Outbox__IntervaloSegundos", "1");
        Environment.SetEnvironmentVariable("Planificador__Habilitado", "false");
        Environment.SetEnvironmentVariable("WhatsApp__SecretoWebhook", SecretoWebhook);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _base.DisposeAsync();
    }

    public async Task<HttpClient> ClienteComoAsync(string codigoInterno)
    {
        var anonimo = CreateClient();
        var respuesta = await anonimo.PostAsJsonAsync("/dev/token", new { codigoInterno });
        respuesta.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("token").GetString());

        return cliente;
    }

    public static async Task<JsonElement> LeerAsync(HttpResponseMessage respuesta)
    {
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.True(respuesta.IsSuccessStatusCode, $"HTTP {(int)respuesta.StatusCode}: {texto}");

        return JsonDocument.Parse(texto).RootElement.Clone();
    }

    /// <summary>Espera a que el outbox procese los eventos: la asignación y las notificaciones son asíncronas.</summary>
    public static async Task<T> EsperarAsync<T>(Func<Task<T?>> consulta, TimeSpan? limite = null)
        where T : struct
    {
        var hasta = DateTime.UtcNow + (limite ?? TimeSpan.FromSeconds(20));

        while (DateTime.UtcNow < hasta)
        {
            if (await consulta() is { } valor)
            {
                return valor;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("La condición esperada no se cumplió a tiempo.");
    }
}
