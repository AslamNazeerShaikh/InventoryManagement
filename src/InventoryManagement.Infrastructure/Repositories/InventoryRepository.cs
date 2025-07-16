using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
{
    private readonly AppDbContext _appDbContext;

    public InventoryRepository(AppDbContext appDbContext)
        : base(appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Inventory?> GetByBarcodeAsync(string barcode)
    {
        return await _appDbContext.Inventories.FirstOrDefaultAsync(x => x.Barcode == barcode);
    }

    public async Task<IEnumerable<Inventory>> GetExpiringInventoriesAsync(DateTime beforeDate)
    {
        return await _appDbContext
            .Inventories.Where(x =>
                x.ExpiryDate.HasValue
                && x.ExpiryDate <= beforeDate
                && x.Status == InventoryStatus.Available
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetAvailableInventoriesAsync()
    {
        return await _appDbContext
            .Inventories.Where(x =>
                x.Status == InventoryStatus.Available && x.AvailableQuantity > 0
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.EquipmentName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetInventoriesByStatusAsync(InventoryStatus status)
    {
        return await _appDbContext
            .Inventories.Where(x => x.Status == status)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.EquipmentName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetInventoriesByCategoryAsync(string category)
    {
        return await _appDbContext
            .Inventories.Where(x => x.Category == category)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.EquipmentName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> SearchInventoriesAsync(string searchTerm)
    {
        return await _appDbContext
            .Inventories.Where(x =>
                x.EquipmentName.Contains(searchTerm)
                || x.Description!.Contains(searchTerm)
                || x.Category!.Contains(searchTerm)
                || x.Brand!.Contains(searchTerm)
                || x.Model!.Contains(searchTerm)
                || x.Barcode!.Contains(searchTerm)
                || x.SerialNumber!.Contains(searchTerm)
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.EquipmentName)
            .ToListAsync();
    }

    public async Task<bool> IsBarcodeExistsAsync(string barcode)
    {
        return await _appDbContext.Inventories.AnyAsync(x => x.Barcode == barcode);
    }

    public async Task<bool> IsSerialNumberExistsAsync(string serialNumber)
    {
        return await _appDbContext.Inventories.AnyAsync(x => x.SerialNumber == serialNumber);
    }

    public async Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 5)
    {
        return await _appDbContext
            .Inventories.Where(x =>
                x.AvailableQuantity <= threshold && x.Status == InventoryStatus.Available
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.AvailableQuantity)
            .ToListAsync();
    }

    public async Task UpdateQuantityAsync(
        int inventoryId,
        int newQuantity,
        int newAvailableQuantity
    )
    {
        var inventory = await _appDbContext.Inventories.FindAsync(inventoryId);
        if (inventory != null)
        {
            inventory.Quantity = newQuantity;
            inventory.AvailableQuantity = newAvailableQuantity;
            _appDbContext.Inventories.Update(inventory);
        }
    }
}
