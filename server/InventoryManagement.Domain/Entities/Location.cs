using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A managed storage location or bin, optionally arranged as a hierarchy (site → room → shelf → bin)
/// via <see cref="ParentLocationId"/>. Promotes the free-text <see cref="Inventory.Location"/> field
/// to a first-class entity while preserving the legacy string for backward compatibility.
/// Domain-agnostic: warehouses, clinics, vehicles, or any physical place stock can live.
/// </summary>
public class Location : BaseEntity
{
    /// <summary>Human-readable location name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short code/label (unique among non-deleted rows when present).</summary>
    public string? Code { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional parent location for hierarchical organisation.</summary>
    public int? ParentLocationId { get; set; }

    /// <summary>Whether the location is currently active/usable.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation to the parent location.</summary>
    public virtual Location? ParentLocation { get; set; }

    /// <summary>Child locations nested under this one.</summary>
    public virtual ICollection<Location> ChildLocations { get; set; } = new List<Location>();

    /// <summary>Inventory items whose current primary location is this one.</summary>
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
