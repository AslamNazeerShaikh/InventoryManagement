namespace InventoryManagement.Domain.Authorization;

/// <summary>
/// Names and default permission grants of the system roles seeded into every tenant. System roles
/// provide sensible out-of-the-box access equivalent to the previous fixed roles; tenants may add,
/// edit and delete their own custom roles on top of these.
/// </summary>
public static class SystemRoles
{
    /// <summary>Full access, including user and role administration. Replaces the former admin flag.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Elevated operational access (manage inventory/stock/assignments). Replaces the former provider flag.</summary>
    public const string Provider = "Provider";

    /// <summary>Standard read-only operational access plus the caller's own assignments.</summary>
    public const string Staff = "Staff";

    /// <summary>All seeded system-role names.</summary>
    public static IReadOnlyList<string> All { get; } =
        new[] { Administrator, Provider, Staff };

    /// <summary>Default role → permission-code grants applied when seeding a tenant.</summary>
    public static IReadOnlyDictionary<string, string[]> DefaultGrants { get; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [Administrator] = Permissions.All.Select(p => p.Code).ToArray(),
            [Provider] = new[]
            {
                Permissions.Inventory.Read,
                Permissions.Inventory.Manage,
                Permissions.Stock.Manage,
                Permissions.Assignments.Read,
                Permissions.Assignments.Manage,
                Permissions.Suppliers.Read,
                Permissions.Suppliers.Manage,
                Permissions.Locations.Read,
                Permissions.Locations.Manage,
                Permissions.Maintenance.Read,
                Permissions.Maintenance.Manage,
                Permissions.Dashboard.Read,
                Permissions.Users.Read,
            },
            [Staff] = new[]
            {
                Permissions.Inventory.Read,
                Permissions.Suppliers.Read,
                Permissions.Locations.Read,
                Permissions.Maintenance.Read,
                Permissions.Dashboard.Read,
            },
        };
}
