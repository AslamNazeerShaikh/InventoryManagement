using System.Security.Claims;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;

namespace InventoryManagement.API.Infrastructure;

/// <summary>
/// Resolves the ambient tenant for the current request from the authenticated principal's
/// <c>tenant</c> claim and pins it on the scoped <see cref="ITenantContext"/> before controllers or
/// the persistence layer execute. Requests with no resolvable tenant claim — anonymous endpoints
/// (login/refresh) or access tokens issued before multi-tenancy existed — fall back to
/// <see cref="TenantConstants.DefaultTenantId"/>, so existing clients keep working unchanged. Must be
/// registered after authentication so the principal's claims are populated.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Creates the middleware.</summary>
    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Resolves and pins the tenant, then invokes the next middleware.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantContext">The request-scoped tenant context to populate.</param>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (
            context.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(context.User.FindFirstValue(AuthConstants.Claims.Tenant), out var tenantId)
            && tenantId != Guid.Empty
        )
        {
            tenantContext.SetTenant(tenantId);
        }
        else
        {
            tenantContext.SetTenant(TenantConstants.DefaultTenantId);
        }

        await _next(context).ConfigureAwait(false);
    }
}
