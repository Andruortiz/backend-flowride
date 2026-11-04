using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using FlowRide.Identidad.Dominio;
using FlowRide.Inicializador.Almacenamiento;
using FlowRide.Inicializador.Configuracion;
using FlowRide.Inicializador.Observabilidad;
using FlowRide.Inicializador.Seguridad;
using FlowRide.Inicializador.TiempoReal;
using FlowRide.Mensajes;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Persistencia;
using FlowRide.Plataforma.Infraestructura;
using FlowRide.Clientes.Infraestructura;
using FlowRide.Identidad.Infraestructura;
using FlowRide.Flota.Infraestructura;
using FlowRide.Solicitudes.Infraestructura;
using FlowRide.Viajes.Infraestructura;
using FlowRide.Asignaciones.Infraestructura;
using FlowRide.Comunicacion.Infraestructura;
using FlowRide.Operacion.Infraestructura;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var esDesarrollo = builder.Environment.IsDevelopment();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(FlowRide.Plataforma.Infraestructura.Controladores.ParametrosController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Clientes.Infraestructura.Controladores.ClientesController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Identidad.Infraestructura.Controladores.UsuariosController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Flota.Infraestructura.Controladores.ConductoresController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Solicitudes.Infraestructura.Controladores.SolicitudesController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Viajes.Infraestructura.Controladores.ViajesController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Asignaciones.Infraestructura.Controladores.AsignacionesController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Comunicacion.Infraestructura.Controladores.AlertasController).Assembly)
    .AddApplicationPart(typeof(FlowRide.Operacion.Infraestructura.Controladores.ReportesController).Assembly)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new ConvertidorFechaUtc());
    });

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

builder.Services.AgregarPersistencia(builder.Configuration, typeof(Program).Assembly.GetName().Name!);
builder.Services.AgregarSeguridad(builder.Configuration);
builder.Services.AgregarAlmacenamiento(builder.Configuration, esDesarrollo);
builder.Services.AgregarCatalogoMensajes();
builder.Services.AgregarModuloPlataforma();
builder.Services.AgregarModuloClientes();
builder.Services.AgregarModuloIdentidad();
builder.Services.AgregarModuloFlota();
builder.Services.AgregarModuloSolicitudes();
builder.Services.AgregarModuloViajes();
builder.Services.AgregarModuloAsignaciones();
builder.Services.AgregarModuloComunicacion();
builder.Services.AgregarModuloOperacion(builder.Configuration);
builder.Services.AgregarTiempoReal();


SymmetricSecurityKey? claveDesarrollo = null;
var authority = builder.Configuration["Jwt:Authority"];

if (string.IsNullOrWhiteSpace(authority))
{
    if (!esDesarrollo)
    {
        throw new InvalidOperationException("Falta 'Jwt:Authority'. Sin proveedor de identidad configurado el servicio no arranca.");
    }

    claveDesarrollo = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.RequireHttpsMetadata = !esDesarrollo;
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(token) && contexto.HttpContext.Request.Path.StartsWithSegments(RegistroTiempoReal.Ruta))
                {
                    contexto.Token = token;
                }

                return Task.CompletedTask;
            },
        };

        if (claveDesarrollo is null)
        {
            o.Authority = authority;
            o.Audience = builder.Configuration["Jwt:Audience"];
        }
        else
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = "flowride-desarrollo",
                ValidAudience = "flowride-api",
                IssuerSigningKey = claveDesarrollo,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        }
    });

builder.Services.AddAuthorization(PoliticasAutorizacion.Registrar);

builder.Services.AddHealthChecks().AddDbContextCheck<FlowRideDbContext>("base-de-datos", tags: ["ready"]);

builder.Services.AgregarObservabilidad(builder.Configuration);

var app = builder.Build();

if (esDesarrollo || app.Configuration.GetValue("Base:MigrarAlIniciar", false))
{
    await using var alcance = app.Services.CreateAsyncScope();
    await alcance.ServiceProvider.GetRequiredService<FlowRideDbContext>().Database.MigrateAsync();
}

if (esDesarrollo)
{
    await SembradoDesarrollo.EjecutarAsync(app.Services);
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseMiddleware<GuardiaTenant>();
app.UseAuthorization();

app.MapControllers();
app.MapearTiempoReal();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") }).AllowAnonymous();

if (claveDesarrollo is not null)
{
    
    app.MapPost("/dev/token", async (SolicitudToken solicitud, FlowRideDbContext db, IContextoTenant tenant) =>
    {
        var usuario = await db.Set<Usuario>().AsNoTracking().FirstOrDefaultAsync(u => u.CodigoInterno == solicitud.CodigoInterno);

        if (usuario is null)
        {
            return Results.NotFound();
        }

        var token = new JwtSecurityToken(
            "flowride-desarrollo",
            "flowride-api",
            [
                new Claim("sub", usuario.Id.ToString()),
                new Claim(Reclamos.UsuarioId, usuario.Id.ToString()),
                new Claim(Reclamos.Tenant, tenant.TenantId.ToString()),
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(claveDesarrollo, SecurityAlgorithms.HmacSha256));

        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }).AllowAnonymous();
}

await app.RunAsync();


public partial class Program;
