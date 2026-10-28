using System.Security.Claims;
using FlowRide.Identidad.Dominio;
using FlowRide.Nucleo.Seguridad;
using FlowRide.Nucleo.Tenancy;
using FlowRide.Persistencia;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Inicializador.Seguridad;

internal static class PoliticasAutorizacion
{
    public static void Registrar(AuthorizationOptions opciones)
    {
        opciones.AddPolicy(Politicas.Autenticado, p => p.RequireClaim(Reclamos.Rol));
        opciones.AddPolicy(Politicas.Administracion, p => p.RequireClaim(Reclamos.Rol, nameof(RolUsuario.AdministradorOperador)));
        opciones.AddPolicy(Politicas.Gestion, p => p.RequireClaim(Reclamos.Rol, nameof(RolUsuario.AdministradorOperador), nameof(RolUsuario.OperadorLogistico)));
        opciones.AddPolicy(Politicas.Conductor, p => p.RequireClaim(Reclamos.Rol, nameof(RolUsuario.Conductor)));
        opciones.AddPolicy(Politicas.Pasajero, p => p.RequireClaim(Reclamos.Rol, nameof(RolUsuario.Pasajero)));
        opciones.AddPolicy(
            Politicas.GestionOConductor,
            p => p.RequireClaim(Reclamos.Rol, nameof(RolUsuario.AdministradorOperador), nameof(RolUsuario.OperadorLogistico), nameof(RolUsuario.Conductor)));
        opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireClaim(Reclamos.Rol).Build();
    }
}

/// <summary>
/// Convierte la identidad del token en el usuario del sistema. El rol sale de la base, no del token, y un usuario
/// desactivado no recibe reclamos: pierde el acceso aunque su token siga vigente.
/// </summary>
internal sealed class TransformadorUsuario(FlowRideDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == Reclamos.Rol))
        {
            return principal;
        }

        Usuario? usuario;

        if (Guid.TryParse(principal.FindFirstValue(Reclamos.UsuarioId), out var id))
        {
            usuario = await db.Set<Usuario>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }
        else if (principal.FindFirstValue("sub") is { } sub)
        {
            usuario = await db.Set<Usuario>().AsNoTracking().FirstOrDefaultAsync(u => u.IdentidadExterna == sub);
        }
        else
        {
            return principal;
        }

        if (usuario is not { Activo: true })
        {
            return principal;
        }

        principal.AddIdentity(new ClaimsIdentity(
        [
            new Claim(Reclamos.UsuarioId, usuario.Id.ToString()),
            new Claim(Reclamos.Rol, usuario.Rol.ToString()),
            new Claim(Reclamos.Nombre, usuario.Nombre),
        ]));

        return principal;
    }
}

internal sealed class ContextoTenant(Guid tenantId) : IContextoTenant
{
    public Guid TenantId { get; } = tenantId;
}

/// <summary>Silo por operador: este proceso atiende un solo tenant. Un token de otro tenant se rechaza (falla cerrado).</summary>
internal sealed class GuardiaTenant(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto, IContextoTenant tenant)
    {
        if (contexto.User.Identity?.IsAuthenticated == true
            && !string.Equals(contexto.User.FindFirstValue(Reclamos.Tenant), tenant.TenantId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;

            return;
        }

        await siguiente(contexto);
    }
}
