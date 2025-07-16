using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

public class InventoryDto
{
    public int Id { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public InventoryStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
    public bool IsExpiryAlertSent { get; set; }
}

public class CreateInventoryDto
{
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInventoryDto
{
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public int Quantity { get; set; }
    public InventoryStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class InventorySearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public InventoryStatus? Status { get; set; }
    public DateTime? ExpiryDateFrom { get; set; }
    public DateTime? ExpiryDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
