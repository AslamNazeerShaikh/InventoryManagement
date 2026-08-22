using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A managed supplier/vendor. Promotes the free-text <see cref="Inventory.Supplier"/> field to a
/// first-class entity with contact details and a reorder lead-time, while the legacy string field is
/// preserved for backward compatibility. Domain-agnostic: any organisation that provides stock.
/// </summary>
public class Supplier : BaseEntity
{
    /// <summary>Supplier/vendor name (unique among non-deleted rows).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional primary contact person.</summary>
    public string? ContactName { get; set; }

    /// <summary>Optional contact email.</summary>
    public string? Email { get; set; }

    /// <summary>Optional contact phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Optional postal address.</summary>
    public string? Address { get; set; }

    /// <summary>Optional website URL.</summary>
    public string? Website { get; set; }

    /// <summary>Optional typical lead time in days between ordering and delivery.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Whether the supplier is currently active/usable.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optional operational notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Inventory items sourced from this supplier.</summary>
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
