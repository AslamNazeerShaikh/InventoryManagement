using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only projection of a managed storage location.</summary>
public class LocationDto
{
    /// <summary>Location identifier.</summary>
    public int Id { get; set; }

    /// <summary>Location name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short code/label.</summary>
    public string? Code { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional parent location identifier.</summary>
    public int? ParentLocationId { get; set; }

    /// <summary>Optional parent location name.</summary>
    public string? ParentLocationName { get; set; }

    /// <summary>Whether the location is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Number of inventory items whose current location is this one.</summary>
    public int ItemCount { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for creating a location.</summary>
public class CreateLocationDto
{
    /// <summary>Location name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short code/label (≤100 chars).</summary>
    [StringLength(100)]
    public string? Code { get; set; }

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional parent location for hierarchy.</summary>
    [Range(1, int.MaxValue)]
    public int? ParentLocationId { get; set; }
}

/// <summary>Payload for updating a location.</summary>
public class UpdateLocationDto
{
    /// <summary>Location name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short code/label (≤100 chars).</summary>
    [StringLength(100)]
    public string? Code { get; set; }

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional parent location for hierarchy.</summary>
    [Range(1, int.MaxValue)]
    public int? ParentLocationId { get; set; }

    /// <summary>Whether the location is active.</summary>
    public bool IsActive { get; set; } = true;
}
