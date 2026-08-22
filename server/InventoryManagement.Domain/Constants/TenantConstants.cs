namespace InventoryManagement.Domain.Constants;

/// <summary>
/// Multi-tenancy constants shared across layers. A single well-known "root" tenant is used to
/// backfill pre-existing rows and to serve requests whose token predates tenant claims, so the
/// system stays fully backward compatible while gaining tenant isolation.
/// </summary>
public static class TenantConstants
{
    /// <summary>
    /// Well-known identifier of the default "root" tenant. Existing data (migrated before
    /// multi-tenancy) and legacy access tokens without a <c>tenant</c> claim resolve to this value,
    /// guaranteeing that current single-tenant deployments continue to work unchanged.
    /// </summary>
    public static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");
}
