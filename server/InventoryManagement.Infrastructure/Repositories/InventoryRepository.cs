using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="Inventory"/> with catalogue-oriented queries.</summary>
public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
{
    /// <summary>Creates the repository.</summary>
    public InventoryRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<Inventory?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Include(x => x.SupplierEntity)
            .Include(x => x.LocationEntity)
            .FirstOrDefaultAsync(x => x.Barcode == barcode, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetExpiringInventoriesAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x =>
                x.ExpiryDate.HasValue
                && x.ExpiryDate <= beforeDate
                && x.Status == InventoryStatus.Available
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetAvailableInventoriesAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.Status == InventoryStatus.Available && x.AvailableQuantity > 0)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetInventoriesByStatusAsync(
        InventoryStatus status,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.Status == status)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetInventoriesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.Category == category)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> SearchInventoriesAsync(
        string searchTerm,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x =>
                x.Name.Contains(searchTerm)
                || (x.Description != null && x.Description.Contains(searchTerm))
                || (x.Category != null && x.Category.Contains(searchTerm))
                || (x.Brand != null && x.Brand.Contains(searchTerm))
                || (x.Model != null && x.Model.Contains(searchTerm))
                || (x.Barcode != null && x.Barcode.Contains(searchTerm))
                || (x.SerialNumber != null && x.SerialNumber.Contains(searchTerm))
            )
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> IsBarcodeExistsAsync(
        string barcode,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AnyAsync(x => x.Barcode == barcode, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> IsSerialNumberExistsAsync(
        string serialNumber,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AnyAsync(x => x.SerialNumber == serialNumber, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetLowStockInventoriesAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.AvailableQuantity <= threshold && x.Status == InventoryStatus.Available)
            .Include(x => x.CreatedByUser)
            .OrderBy(x => x.AvailableQuantity)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Inventory>> GetReorderInventoriesAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x =>
                x.ReorderLevel != null
                && x.AvailableQuantity <= x.ReorderLevel
                && x.Status == InventoryStatus.Available
            )
            .Include(x => x.CreatedByUser)
            .Include(x => x.SupplierEntity)
            .Include(x => x.LocationEntity)
            .OrderBy(x => x.AvailableQuantity)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> GetCountsBySupplierAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.SupplierId != null)
            .GroupBy(x => x.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> GetCountsByLocationAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.LocationId != null)
            .GroupBy(x => x.LocationId!.Value)
            .Select(g => new { LocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LocationId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);
}
