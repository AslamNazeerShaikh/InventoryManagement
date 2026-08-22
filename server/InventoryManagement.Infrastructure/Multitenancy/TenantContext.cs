using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;

namespace InventoryManagement.Infrastructure.Multitenancy;

/// <summary>
/// Default request-scoped <see cref="ITenantContext"/>. Registered as scoped so each request gets an
/// isolated instance; the presentation layer resolves the tenant from the authenticated principal
/// and calls <see cref="SetTenant"/> exactly once. Until then it reports
/// <see cref="TenantConstants.DefaultTenantId"/>, which keeps seed/design-time/background contexts
/// and legacy tokens working as the default tenant.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private Guid _tenantId = TenantConstants.DefaultTenantId;

    /// <inheritdoc />
    public Guid TenantId => _tenantId;

    /// <inheritdoc />
    public bool IsResolved { get; private set; }

    /// <inheritdoc />
    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId == Guid.Empty ? TenantConstants.DefaultTenantId : tenantId;
        IsResolved = true;
    }
}
