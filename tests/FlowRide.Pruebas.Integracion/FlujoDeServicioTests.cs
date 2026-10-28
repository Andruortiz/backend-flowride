using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlowRide.Pruebas.Integracion;

[Collection(ColeccionApi.Nombre)]
public sealed class FlujoDeServicioTests(FabricaApi api)
{
    private static object NuevaSolicitud(int horas = 3) => new
    {
        origen = "Sede Norte",
        destino = "Aeropuerto",
        fechaHoraProgramada = DateTimeOffset.UtcNow.AddHours(horas),
        urgente = false,
        esPersonal = false,
    };

    private static async Task<JsonElement?> SolicitudAsignadaAsync(HttpClient cliente, string id)
    {
        var json = await FabricaApi.LeerAsync(await cliente.GetAsync($"/api/v1/solicitudes/{id}"));

        return json.GetProperty("estado").GetString() == "Asignada" ? json : null;
    }

    [Fact]
    public async Task UnaSolicitudSeAsignaSolaYRecorreTodoElCiclo()
    {
        var pasajero = await api.ClienteComoAsync("PAS-001");
        var creada = await FabricaApi.LeerAsync(await pasajero.PostAsJsonAsync("/api/v1/solicitudes", NuevaSolicitud()));
        var solicitudId = creada.GetProperty("id").GetString()!;
        Assert.Equal("Pendiente", creada.GetProperty("estado").GetString());

        var asignada = await FabricaApi.EsperarAsync(() => SolicitudAsignadaAsync(pasajero, solicitudId));
        var asignacionId = asignada.GetProperty("asignacionId").GetString()!;

        var gestion = await api.ClienteComoAsync("OPL-001");
        var asignacion = await FabricaApi.LeerAsync(await gestion.GetAsync($"/api/v1/asignaciones/{asignacionId}"));
        Assert.Equal("PendienteAceptacion", asignacion.GetProperty("estado").GetString());
        Assert.NotEqual(0, asignacion.GetProperty("explicacion").GetProperty("ranking").GetArrayLength());

        var conductor = await ConductorDeAsync(asignacion.GetProperty("conductorId").GetString()!);
        await FabricaApi.LeerAsync(await conductor.PostAsync($"/api/v1/asignaciones/{asignacionId}/aceptar", null));

        var viaje = await FabricaApi.EsperarAsync(async () =>
        {
            var lista = await FabricaApi.LeerAsync(await conductor.GetAsync("/api/v1/viajes?estado=Programado"));

            return lista.GetArrayLength() > 0 ? lista[0] : (JsonElement?)null;
        });
        var viajeId = viaje.GetProperty("id").GetString()!;

        await FabricaApi.LeerAsync(await conductor.PostAsync($"/api/v1/viajes/{viajeId}/iniciar", null));
        await FabricaApi.LeerAsync(await conductor.PostAsync($"/api/v1/viajes/{viajeId}/finalizar", null));
        await FabricaApi.LeerAsync(await pasajero.PostAsJsonAsync($"/api/v1/viajes/{viajeId}/calificacion", new { puntaje = 5, comentario = "Excelente" }));

        // La solicitud pasa a Finalizada al procesarse el evento ViajeFinalizado (consistencia eventual entre módulos).
        await FabricaApi.EsperarAsync(async () =>
        {
            var actual = await FabricaApi.LeerAsync(await pasajero.GetAsync($"/api/v1/solicitudes/{solicitudId}"));

            return actual.GetProperty("estado").GetString() == "Finalizada" ? actual : (JsonElement?)null;
        });

        var repetida = await pasajero.PostAsJsonAsync($"/api/v1/viajes/{viajeId}/calificacion", new { puntaje = 4 });
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task DosReasignacionesSimultaneasNoDejanDosAsignacionesVivas()
    {
        var pasajero = await api.ClienteComoAsync("PAS-002");
        var creada = await FabricaApi.LeerAsync(await pasajero.PostAsJsonAsync("/api/v1/solicitudes", NuevaSolicitud(4)));
        var solicitudId = creada.GetProperty("id").GetString()!;
        await FabricaApi.EsperarAsync(() => SolicitudAsignadaAsync(pasajero, solicitudId));

        var gestion = await api.ClienteComoAsync("OPL-001");
        var conductores = await FabricaApi.LeerAsync(await gestion.GetAsync("/api/v1/conductores"));
        var ids = conductores.EnumerateArray().Select(c => c.GetProperty("id").GetString()!).ToList();

        var respuestas = await Task.WhenAll(
            gestion.PostAsJsonAsync($"/api/v1/solicitudes/{solicitudId}/reasignar", new { conductorId = ids[0], motivo = "Cambio por operación A" }),
            gestion.PostAsJsonAsync($"/api/v1/solicitudes/{solicitudId}/reasignar", new { conductorId = ids[1], motivo = "Cambio por operación B" }));

        var codigos = respuestas.Select(r => r.StatusCode).ToList();
        Assert.Contains(HttpStatusCode.OK, codigos);
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, codigos);

        var lista = await FabricaApi.LeerAsync(await gestion.GetAsync("/api/v1/asignaciones?estado=PendienteAceptacion&tamano=200"));
        Assert.Equal(1, lista.EnumerateArray().Count(a => a.GetProperty("solicitudId").GetString() == solicitudId));
    }

    [Fact]
    public async Task UnPasajeroNoPuedeVerLaSolicitudDeOtro()
    {
        var uno = await api.ClienteComoAsync("PAS-001");
        var otro = await api.ClienteComoAsync("PAS-002");
        var creada = await FabricaApi.LeerAsync(await uno.PostAsJsonAsync("/api/v1/solicitudes", NuevaSolicitud(5)));

        var respuesta = await otro.GetAsync($"/api/v1/solicitudes/{creada.GetProperty("id").GetString()}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task LaGestionNoVeOrigenNiDestinoDeUnServicioPersonal()
    {
        var pasajero = await api.ClienteComoAsync("PAS-001");
        var creada = await FabricaApi.LeerAsync(await pasajero.PostAsJsonAsync("/api/v1/solicitudes", new
        {
            origen = "Mi casa",
            destino = "Clínica",
            fechaHoraProgramada = DateTimeOffset.UtcNow.AddHours(6),
            urgente = false,
            esPersonal = true,
        }));

        var gestion = await api.ClienteComoAsync("OPL-001");
        var vista = await FabricaApi.LeerAsync(await gestion.GetAsync($"/api/v1/solicitudes/{creada.GetProperty("id").GetString()}"));

        Assert.Equal(JsonValueKind.Null, vista.GetProperty("origen").ValueKind);
        Assert.Equal(JsonValueKind.Null, vista.GetProperty("destino").ValueKind);
    }

    [Fact]
    public async Task ElPasajeroNoCancelaDentroDeLaVentanaMinima()
    {
        var pasajero = await api.ClienteComoAsync("PAS-001");
        var creada = await FabricaApi.LeerAsync(await pasajero.PostAsJsonAsync("/api/v1/solicitudes", new
        {
            origen = "A",
            destino = "B",
            fechaHoraProgramada = DateTimeOffset.UtcNow.AddMinutes(20),
            urgente = true,
            esPersonal = false,
        }));

        var respuesta = await pasajero.PostAsJsonAsync($"/api/v1/solicitudes/{creada.GetProperty("id").GetString()}/cancelar", new { motivo = "Ya no voy" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    private async Task<HttpClient> ConductorDeAsync(string conductorId)
    {
        foreach (var codigo in new[] { "CON-001", "CON-002", "CON-003" })
        {
            var cliente = await api.ClienteComoAsync(codigo);
            var lista = await FabricaApi.LeerAsync(await cliente.GetAsync("/api/v1/asignaciones?tamano=200"));

            if (lista.EnumerateArray().Any(a => a.GetProperty("conductorId").GetString() == conductorId))
            {
                return cliente;
            }
        }

        throw new InvalidOperationException("Ningún conductor sembrado tiene esa asignación.");
    }
}
