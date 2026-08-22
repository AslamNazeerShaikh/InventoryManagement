namespace InventoryManagement.Domain.Common;

/// <summary>
/// Ambient, request-scoped tenant context. Resolved once per request (from the authenticated
/// principal's <c>tenant</c> claim) and consumed by the persistence layer to (a) filter every
/// query to the caller's tenant and (b) stamp the tenant on newly inserted rows. Kept in the
/// Domain layer so it carries no framework/transport dependency and remains reusable.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant identifier. Falls back to
    /// <see cref="Constants.TenantConstants.DefaultTenantId"/> until explicitly resolved, so
    /// background/seed/design-time contexts (which never call <see cref="SetTenant"/>) behave as
    /// the default tenant instead of failing.
    /// </summary>
    Guid TenantId { get; }

    /// <summary><c>true</c> once <see cref="SetTenant"/> has been called for the current scope.</summary>
    bool IsResolved { get; }

    /// <summary>Pins the tenant for the current scope. Intended to be called once, early in the request.</summary>
    /// <param name="tenantId">The resolved tenant identifier.</param>
    void SetTenant(Guid tenantId);
}
