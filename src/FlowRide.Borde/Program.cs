using System.Threading.RateLimiting;
using FlowRide.Borde;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var maxBytes = builder.Configuration.GetValue("Borde:MaxBytesCuerpo", 1_048_576L);
var limitePorMinuto = builder.Configuration.GetValue("Borde:LimitePorMinuto", 300);

builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = maxBytes);

builder.Services.Configure<OpcionesBorde>(builder.Configuration.GetSection(OpcionesBorde.Seccion));
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});
builder.Services.AddHttpClient(RouterWebhooks.Cliente, c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("por-ip", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = limitePorMinuto, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.Use(async (contexto, siguiente) =>
{
    var cabeceras = contexto.Response.Headers;
    cabeceras.XContentTypeOptions = "nosniff";
    cabeceras.XFrameOptions = "DENY";
    cabeceras["Referrer-Policy"] = "no-referrer";

    if (contexto.Request.IsHttps)
    {
        cabeceras.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
    }

    await siguiente();
});
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapWebhooksMeta();
app.MapReverseProxy();

await app.RunAsync();

public partial class Program;
