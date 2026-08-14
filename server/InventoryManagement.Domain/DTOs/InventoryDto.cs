using System.ComponentModel.DataAnnotations;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only inventory projection returned by the API.</summary>
public class InventoryDto
{
    /// <summary>Item identifier.</summary>
    public int Id { get; set; }

    /// <summary>Equipment name.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Optional brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Optional model.</summary>
    public string? Model { get; set; }

    /// <summary>Optional serial number.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Optional barcode.</summary>
    public string? Barcode { get; set; }

    /// <summary>Optional expiry date.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Optional manufacture date.</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Optional purchase price.</summary>
    public decimal? PurchasePrice { get; set; }

    /// <summary>Optional supplier.</summary>
    public string? Supplier { get; set; }

    /// <summary>Total quantity owned.</summary>
    public int Quantity { get; set; }

    /// <summary>Quantity currently available.</summary>
    public int AvailableQuantity { get; set; }

    /// <summary>Lifecycle status.</summary>
    public InventoryStatus Status { get; set; }

    /// <summary>Optional storage location.</summary>
    public string? Location { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Name of the creating user, if known.</summary>
    public string? CreatedByUserName { get; set; }

    /// <summary>Whether an expiry alert has been sent.</summary>
    public bool IsExpiryAlertSent { get; set; }
}

/// <summary>Payload for creating an inventory item.</summary>
public class CreateInventoryDto
{
    /// <summary>Equipment name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional category (≤100 chars).</summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>Optional brand (≤100 chars).</summary>
    [StringLength(100)]
    public string? Brand { get; set; }

    /// <summary>Optional model (≤100 chars).</summary>
    [StringLength(100)]
    public string? Model { get; set; }

    /// <summary>Optional serial number (≤50 chars).</summary>
    [StringLength(50)]
    public string? SerialNumber { get; set; }

    /// <summary>Optional barcode (≤50 chars).</summary>
    [StringLength(50)]
    public string? Barcode { get; set; }

    /// <summary>Optional expiry date.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Optional manufacture date.</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Optional purchase price (non-negative).</summary>
    [Range(0, double.MaxValue)]
    public decimal? PurchasePrice { get; set; }

    /// <summary>Optional supplier (≤200 chars).</summary>
    [StringLength(200)]
    public string? Supplier { get; set; }

    /// <summary>Total quantity (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    /// <summary>Optional storage location (≤200 chars).</summary>
    [StringLength(200)]
    public string? Location { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for updating an inventory item.</summary>
public class UpdateInventoryDto
{
    /// <summary>Equipment name (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional category (≤100 chars).</summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>Optional brand (≤100 chars).</summary>
    [StringLength(100)]
    public string? Brand { get; set; }

    /// <summary>Optional model (≤100 chars).</summary>
    [StringLength(100)]
    public string? Model { get; set; }

    /// <summary>Optional serial number (≤50 chars).</summary>
    [StringLength(50)]
    public string? SerialNumber { get; set; }

    /// <summary>Optional barcode (≤50 chars).</summary>
    [StringLength(50)]
    public string? Barcode { get; set; }

    /// <summary>Optional expiry date.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Optional manufacture date.</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Optional purchase price (non-negative).</summary>
    [Range(0, double.MaxValue)]
    public decimal? PurchasePrice { get; set; }

    /// <summary>Optional supplier (≤200 chars).</summary>
    [StringLength(200)]
    public string? Supplier { get; set; }

    /// <summary>Total quantity (≥0).</summary>
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>Lifecycle status.</summary>
    [EnumDataType(typeof(InventoryStatus))]
    public InventoryStatus Status { get; set; }

    /// <summary>Optional storage location (≤200 chars).</summary>
    [StringLength(200)]
    public string? Location { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Search/filter criteria for inventory queries.</summary>
public class InventorySearchDto
{
    /// <summary>Optional free-text term matched across key fields.</summary>
    [StringLength(200)]
    public string? SearchTerm { get; set; }

    /// <summary>Optional category filter.</summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>Optional status filter.</summary>
    public InventoryStatus? Status { get; set; }

    /// <summary>Optional lower bound on expiry date.</summary>
    public DateTime? ExpiryDateFrom { get; set; }

    /// <summary>Optional upper bound on expiry date.</summary>
    public DateTime? ExpiryDateTo { get; set; }

    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
