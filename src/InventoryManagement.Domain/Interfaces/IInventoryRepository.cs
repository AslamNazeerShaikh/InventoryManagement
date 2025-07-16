using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Interfaces;

public interface IInventoryRepository : IGenericRepository<Inventory>
{
    Task<Inventory?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Inventory>> GetExpiringInventoriesAsync(DateTime beforeDate);
    Task<IEnumerable<Inventory>> GetAvailableInventoriesAsync();
    Task<IEnumerable<Inventory>> GetInventoriesByStatusAsync(InventoryStatus status);
    Task<IEnumerable<Inventory>> GetInventoriesByCategoryAsync(string category);
    Task<IEnumerable<Inventory>> SearchInventoriesAsync(string searchTerm);
    Task<bool> IsBarcodeExistsAsync(string barcode);
    Task<bool> IsSerialNumberExistsAsync(string serialNumber);
    Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 5);
    Task UpdateQuantityAsync(int inventoryId, int newQuantity, int newAvailableQuantity);
}
