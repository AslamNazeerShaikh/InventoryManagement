using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only projection of a managed supplier/vendor.</summary>
public class SupplierDto
{
    /// <summary>Supplier identifier.</summary>
    public int Id { get; set; }

    /// <summary>Supplier name.</summary>
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

    /// <summary>Optional typical lead time in days.</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Whether the supplier is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Number of inventory items sourced from this supplier.</summary>
    public int ItemCount { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for creating a supplier.</summary>
public class CreateSupplierDto
{
    /// <summary>Supplier name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional contact person (≤200 chars).</summary>
    [StringLength(200)]
    public string? ContactName { get; set; }

    /// <summary>Optional contact email (≤256 chars).</summary>
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    /// <summary>Optional contact phone (≤50 chars).</summary>
    [StringLength(50)]
    public string? Phone { get; set; }

    /// <summary>Optional postal address (≤500 chars).</summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>Optional website URL (≤500 chars).</summary>
    [StringLength(500)]
    [Url]
    public string? Website { get; set; }

    /// <summary>Optional lead time in days (non-negative).</summary>
    [Range(0, int.MaxValue)]
    public int? LeadTimeDays { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for updating a supplier.</summary>
public class UpdateSupplierDto
{
    /// <summary>Supplier name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional contact person (≤200 chars).</summary>
    [StringLength(200)]
    public string? ContactName { get; set; }

    /// <summary>Optional contact email (≤256 chars).</summary>
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    /// <summary>Optional contact phone (≤50 chars).</summary>
    [StringLength(50)]
    public string? Phone { get; set; }

    /// <summary>Optional postal address (≤500 chars).</summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>Optional website URL (≤500 chars).</summary>
    [StringLength(500)]
    [Url]
    public string? Website { get; set; }

    /// <summary>Optional lead time in days (non-negative).</summary>
    [Range(0, int.MaxValue)]
    public int? LeadTimeDays { get; set; }

    /// <summary>Whether the supplier is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
