namespace InventoryManagement.Domain.Authorization;

/// <summary>A seedable permission definition (code + UI grouping + human description).</summary>
/// <param name="Code">Stable machine code checked by authorization, e.g. <c>inventory.manage</c>.</param>
/// <param name="Category">UI grouping, e.g. <c>Inventory</c>.</param>
/// <param name="Description">Human-readable description shown in role-management UIs.</param>
public sealed record PermissionDefinition(string Code, string Category, string Description);

/// <summary>
/// Canonical catalog of permission codes enforced by the API. These are seeded into every tenant so
/// tenant administrators can compose them into custom roles; tenants may additionally define their
/// own permissions for client-side feature gating. Codes follow a stable <c>resource.action</c>
/// convention and are the single source of truth shared by controllers and seeding.
/// </summary>
public static class Permissions
{
    /// <summary>Inventory catalogue permissions.</summary>
    public static class Inventory
    {
        /// <summary>View inventory items.</summary>
        public const string Read = "inventory.read";

        /// <summary>Create and edit inventory items.</summary>
        public const string Manage = "inventory.manage";

        /// <summary>Delete inventory items.</summary>
        public const string Delete = "inventory.delete";
    }

    /// <summary>Stock-movement permissions.</summary>
    public static class Stock
    {
        /// <summary>Receive, adjust, dispose and transfer stock.</summary>
        public const string Manage = "stock.manage";
    }

    /// <summary>Assignment permissions.</summary>
    public static class Assignments
    {
        /// <summary>View assignments belonging to any user.</summary>
        public const string Read = "assignments.read";

        /// <summary>Create, update, return and renew assignments.</summary>
        public const string Manage = "assignments.manage";
    }

    /// <summary>Supplier permissions.</summary>
    public static class Suppliers
    {
        /// <summary>View suppliers.</summary>
        public const string Read = "suppliers.read";

        /// <summary>Create and edit suppliers.</summary>
        public const string Manage = "suppliers.manage";
    }

    /// <summary>Location permissions.</summary>
    public static class Locations
    {
        /// <summary>View locations.</summary>
        public const string Read = "locations.read";

        /// <summary>Create and edit locations.</summary>
        public const string Manage = "locations.manage";
    }

    /// <summary>Maintenance permissions.</summary>
    public static class Maintenance
    {
        /// <summary>View maintenance schedules.</summary>
        public const string Read = "maintenance.read";

        /// <summary>Create, edit and complete maintenance schedules.</summary>
        public const string Manage = "maintenance.manage";
    }

    /// <summary>Dashboard permissions.</summary>
    public static class Dashboard
    {
        /// <summary>View the dashboard and its aggregates.</summary>
        public const string Read = "dashboard.read";
    }

    /// <summary>User-management permissions.</summary>
    public static class Users
    {
        /// <summary>View users (e.g. to select an assignment recipient).</summary>
        public const string Read = "users.read";

        /// <summary>Create, edit and delete users, including their role membership.</summary>
        public const string Manage = "users.manage";
    }

    /// <summary>Role/permission administration.</summary>
    public static class Roles
    {
        /// <summary>Create, edit and delete roles and their permission grants.</summary>
        public const string Manage = "roles.manage";
    }

    /// <summary>Every catalog permission with its description/category, used when seeding a tenant.</summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } =
        new List<PermissionDefinition>
        {
            new(Inventory.Read, "Inventory", "View inventory items"),
            new(Inventory.Manage, "Inventory", "Create and edit inventory items"),
            new(Inventory.Delete, "Inventory", "Delete inventory items"),
            new(Stock.Manage, "Inventory", "Receive, adjust, dispose and transfer stock"),
            new(Assignments.Read, "Assignments", "View all assignments"),
            new(Assignments.Manage, "Assignments", "Create, update and return assignments"),
            new(Suppliers.Read, "Suppliers", "View suppliers"),
            new(Suppliers.Manage, "Suppliers", "Create and edit suppliers"),
            new(Locations.Read, "Locations", "View locations"),
            new(Locations.Manage, "Locations", "Create and edit locations"),
            new(Maintenance.Read, "Maintenance", "View maintenance schedules"),
            new(Maintenance.Manage, "Maintenance", "Create, edit and complete maintenance"),
            new(Dashboard.Read, "Dashboard", "View the dashboard"),
            new(Users.Read, "Users", "View users"),
            new(Users.Manage, "Users", "Create, edit and delete users"),
            new(Roles.Manage, "Roles", "Manage roles and permissions"),
        };
}
