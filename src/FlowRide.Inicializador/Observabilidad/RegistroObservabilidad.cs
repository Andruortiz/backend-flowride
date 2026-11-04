using Azure.Monitor.OpenTelemetry.AspNetCore;
using FlowRide.Nucleo.Observabilidad;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FlowRide.Inicializador.Observabilidad;

internal static class RegistroObservabilidad
{
    public const string CadenaAzureMonitor = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public static IServiceCollection AgregarObservabilidad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var hayOtlp = !string.IsNullOrWhiteSpace(configuracion["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        var hayAzure = !string.IsNullOrWhiteSpace(configuracion[CadenaAzureMonitor]);

        var telemetria = servicios.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("flowride-backend"))
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource("Npgsql");

                if (hayOtlp)
                {
                    t.AddOtlpExporter();
                }
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddMeter(Metricas.Medidor);

                if (hayOtlp)
                {
                    m.AddOtlpExporter();
                }
            });

        if (hayAzure)
        {
            telemetria.UseAzureMonitor();
        }

        return servicios;
    }
}
