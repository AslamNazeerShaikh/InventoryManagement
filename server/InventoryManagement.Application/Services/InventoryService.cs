using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>Inventory catalogue service. Executes filtering, ordering and paging in SQL and keeps
/// the available-quantity invariant consistent. Unexpected faults bubble to the global handler.</summary>
public sealed class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    /// <summary>Creates the inventory service.</summary>
    public InventoryService(IUnitOfWork unitOfWork, ILogger<InventoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetAllInventoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var inventories = await _unitOfWork
            .Inventories.ListAsync(
                include: q =>
                    q.Include(x => x.CreatedByUser)
                        .Include(x => x.SupplierEntity)
                        .Include(x => x.LocationEntity),
                orderBy: q => q.OrderBy(x => x.EquipmentName),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> GetInventoryByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await LoadWithCreatorAsync(id, cancellationToken).ConfigureAwait(false);
        return inventory is null
            ? Result<InventoryDto>.NotFound("Inventory not found")
            : Result<InventoryDto>.Success(inventory.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> GetInventoryByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await _unitOfWork
            .Inventories.GetByBarcodeAsync(barcode, cancellationToken)
            .ConfigureAwait(false);
        return inventory is null
            ? Result<InventoryDto>.NotFound("Inventory not found")
            : Result<InventoryDto>.Success(inventory.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> CreateInventoryAsync(
        CreateInventoryDto createInventoryDto,
        int createdByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !string.IsNullOrEmpty(createInventoryDto.Barcode)
            && await _unitOfWork
                .Inventories.IsBarcodeExistsAsync(createInventoryDto.Barcode, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<InventoryDto>.Conflict("Barcode already exists");
        }

        if (
            !string.IsNullOrEmpty(createInventoryDto.SerialNumber)
            && await _unitOfWork
                .Inventories.IsSerialNumberExistsAsync(
                    createInventoryDto.SerialNumber,
                    cancellationToken
                )
                .ConfigureAwait(false)
        )
        {
            return Result<InventoryDto>.Conflict("Serial number already exists");
        }

        var inventory = createInventoryDto.ToEntity();
        inventory.CreatedByUserId = createdByUserId;
        inventory.Status = InventoryStatus.Available;

        await _unitOfWork.Inventories.AddAsync(inventory, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created inventory {InventoryId}.", inventory.Id);
        var created = await LoadWithCreatorAsync(inventory.Id, cancellationToken)
            .ConfigureAwait(false);
        return Result<InventoryDto>.Success(
            created!.ToDto(),
            "Inventory created successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> UpdateInventoryAsync(
        int id,
        UpdateInventoryDto updateInventoryDto,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await _unitOfWork
            .Inventories.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (inventory is null)
        {
            return Result<InventoryDto>.NotFound("Inventory not found");
        }

        if (
            !string.IsNullOrEmpty(updateInventoryDto.Barcode)
            && inventory.Barcode != updateInventoryDto.Barcode
            && await _unitOfWork
                .Inventories.IsBarcodeExistsAsync(updateInventoryDto.Barcode, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<InventoryDto>.Conflict("Barcode already exists");
        }

        if (
            !string.IsNullOrEmpty(updateInventoryDto.SerialNumber)
            && inventory.SerialNumber != updateInventoryDto.SerialNumber
            && await _unitOfWork
                .Inventories.IsSerialNumberExistsAsync(
                    updateInventoryDto.SerialNumber,
                    cancellationToken
                )
                .ConfigureAwait(false)
        )
        {
            return Result<InventoryDto>.Conflict("Serial number already exists");
        }

        // Keep AvailableQuantity consistent with the total-quantity delta.
        var quantityDifference = updateInventoryDto.Quantity - inventory.Quantity;
        var newAvailableQuantity = inventory.AvailableQuantity + quantityDifference;
        if (newAvailableQuantity < 0)
        {
            return Result<InventoryDto>.Validation("Cannot reduce quantity below assigned amount");
        }

        updateInventoryDto.UpdateEntity(inventory);
        inventory.AvailableQuantity = newAvailableQuantity;

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated inventory {InventoryId}.", id);
        var updated = await LoadWithCreatorAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<InventoryDto>.Success(
            updated!.ToDto(),
            "Inventory updated successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteInventoryAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await _unitOfWork
            .Inventories.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (inventory is null)
        {
            return Result<bool>.NotFound("Inventory not found");
        }

        var hasActiveAssignments = await _unitOfWork
            .InventoryAssignments.AnyAsync(
                x => x.InventoryId == id && x.Status == AssignmentStatus.Active,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (hasActiveAssignments)
        {
            return Result<bool>.Conflict("Cannot delete inventory with active assignments");
        }

        inventory.IsDeleted = true;
        inventory.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted inventory {InventoryId}.", id);
        return Result<bool>.Success(true, "Inventory deleted successfully");
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetAvailableInventoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var inventories = await _unitOfWork
            .Inventories.GetAvailableInventoriesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetExpiringInventoriesAsync(
        int monthsBefore = 3,
        CancellationToken cancellationToken = default
    )
    {
        var expiryDate = DateTime.UtcNow.AddMonths(monthsBefore);
        var inventories = await _unitOfWork
            .Inventories.GetExpiringInventoriesAsync(expiryDate, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetLowStockInventoriesAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default
    )
    {
        var inventories = await _unitOfWork
            .Inventories.GetLowStockInventoriesAsync(threshold, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetReorderInventoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var inventories = await _unitOfWork
            .Inventories.GetReorderInventoriesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> SearchInventoriesAsync(
        InventorySearchDto searchDto,
        CancellationToken cancellationToken = default
    )
    {
        var term = searchDto.SearchTerm;
        var category = searchDto.Category;
        var status = searchDto.Status;
        var from = searchDto.ExpiryDateFrom;
        var to = searchDto.ExpiryDateTo;

        // Single SQL query: all predicates translated server-side (no in-memory filtering).
        var inventories = await _unitOfWork
            .Inventories.ListAsync(
                predicate: x =>
                    (
                        term == null
                        || term == ""
                        || x.EquipmentName.Contains(term)
                        || (x.Description != null && x.Description.Contains(term))
                        || (x.Category != null && x.Category.Contains(term))
                        || (x.Brand != null && x.Brand.Contains(term))
                        || (x.Model != null && x.Model.Contains(term))
                        || (x.Barcode != null && x.Barcode.Contains(term))
                        || (x.SerialNumber != null && x.SerialNumber.Contains(term))
                    )
                    && (category == null || category == "" || x.Category == category)
                    && (status == null || x.Status == status)
                    && (from == null || (x.ExpiryDate != null && x.ExpiryDate >= from))
                    && (to == null || (x.ExpiryDate != null && x.ExpiryDate <= to)),
                include: q =>
                    q.Include(x => x.CreatedByUser)
                        .Include(x => x.SupplierEntity)
                        .Include(x => x.LocationEntity),
                orderBy: q => q.OrderBy(x => x.EquipmentName),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<InventoryDto>>> GetInventoriesPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .Inventories.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                include: q =>
                    q.Include(x => x.CreatedByUser)
                        .Include(x => x.SupplierEntity)
                        .Include(x => x.LocationEntity),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<InventoryDto>
        {
            Data = page.Items.ToDto(),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return Result<PagedResult<InventoryDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryDto>>> GetInventoriesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default
    )
    {
        var inventories = await _unitOfWork
            .Inventories.GetInventoriesByCategoryAsync(category, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <summary>Loads a single inventory item (tracking-free) with its creator navigation for projection.</summary>
    private Task<Inventory?> LoadWithCreatorAsync(int id, CancellationToken cancellationToken) =>
        _unitOfWork.Inventories.FirstOrDefaultAsync(
            x => x.Id == id,
            include: q =>
                q.Include(i => i.CreatedByUser)
                    .Include(i => i.SupplierEntity)
                    .Include(i => i.LocationEntity),
            cancellationToken: cancellationToken
        );
}
