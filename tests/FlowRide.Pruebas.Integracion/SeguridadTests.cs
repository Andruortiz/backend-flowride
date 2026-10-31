using System.Net;
using System.Net.Http.Json;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class SeguridadTests(FabricaApi api)
{
    [Fact]
    public async Task SinTokenSeRechaza()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/usuarios");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnPasajeroNoAccedeALaAdministracion()
    {
        var pasajero = await api.ClienteComoAsync("PAS-001");

        Assert.Equal(HttpStatusCode.Forbidden, (await pasajero.GetAsync("/api/v1/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await pasajero.GetAsync("/api/v1/auditoria")).StatusCode);
    }

    [Fact]
    public async Task LaOperacionLogisticaNoAdministraUsuarios()
    {
        var gestion = await api.ClienteComoAsync("OPL-001");

        var respuesta = await gestion.PostAsJsonAsync("/api/v1/usuarios", new { nombre = "X", correo = "x@x.co", codigoInterno = "X-1", rol = "OperadorLogistico" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnUsuarioDesactivadoPierdeElAccesoAunConTokenVigente()
    {
        var admin = await api.ClienteComoAsync("ADM-001");
        var pasajero = await api.ClienteComoAsync("PAS-002");
        Assert.Equal(HttpStatusCode.OK, (await pasajero.GetAsync("/api/v1/solicitudes")).StatusCode);

        var usuarios = await FabricaApi.LeerAsync(await admin.GetAsync("/api/v1/usuarios?rol=Pasajero"));
        var id = usuarios.EnumerateArray().First(u => u.GetProperty("codigoInterno").GetString() == "PAS-002").GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/usuarios/{id}/desactivar", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await pasajero.GetAsync("/api/v1/solicitudes")).StatusCode);

        await admin.PostAsync($"/api/v1/usuarios/{id}/activar", null);
    }

    [Fact]
    public async Task ElWebhookExigeElSecretoCompartido()
    {
        var cliente = api.CreateClient();
        var mensaje = new { externalMessageId = "wamid.prueba-1", telefono = "573000000101", texto = "estado" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/v1/webhooks/whatsapp", mensaje)).StatusCode);

        cliente.DefaultRequestHeaders.Add("X-Webhook-Secret", "incorrecto");
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/v1/webhooks/whatsapp", mensaje)).StatusCode);
    }

    [Fact]
    public async Task UnMensajeRepetidoDeWhatsAppSeProcesaUnaSolaVez()
    {
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Webhook-Secret", FabricaApi.SecretoWebhook);
        var mensaje = new { externalMessageId = "wamid.prueba-2", telefono = "573000000101", texto = "estado" };

        var primero = await FabricaApi.LeerAsync(await cliente.PostAsJsonAsync("/api/v1/webhooks/whatsapp", mensaje));
        var segundo = await FabricaApi.LeerAsync(await cliente.PostAsJsonAsync("/api/v1/webhooks/whatsapp", mensaje));

        Assert.NotEmpty(primero.GetProperty("texto").GetString()!);
        Assert.Equal(string.Empty, segundo.GetProperty("texto").GetString());
    }

    [Fact]
    public async Task LaSaludDeLaBaseEstaDisponibleSinToken()
    {
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.CreateClient().GetAsync("/health/ready")).StatusCode);
    }
}
