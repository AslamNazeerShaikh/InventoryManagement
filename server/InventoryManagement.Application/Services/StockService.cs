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

/// <summary>
/// Stock service. Every quantity- or location-changing operation runs inside a transaction and
/// appends exactly one <see cref="StockMovement"/> ledger row, so an item's lifecycle is fully
/// auditable. The entity concurrency token prevents lost updates / oversell under concurrency;
/// conflicts surface as HTTP 409 via the unit of work.
/// </summary>
public sealed class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StockService> _logger;

    /// <summary>Creates the stock service.</summary>
    public StockService(IUnitOfWork unitOfWork, ILogger<StockService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> ReceiveStockAsync(
        int inventoryId,
        ReceiveStockDto receiveDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork
                        .Inventories.GetByIdAsync(inventoryId, ct)
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found", ResultErrorType.NotFound);
                    }

                    if (inventory.Status == InventoryStatus.Disposed)
                    {
                        return Failed(
                            "Cannot receive stock for a disposed item",
                            ResultErrorType.Validation
                        );
                    }

                    if (
                        receiveDto.SupplierId is int supplierId
                        && !await _unitOfWork
                            .Suppliers.AnyAsync(s => s.Id == supplierId, ct)
                            .ConfigureAwait(false)
                    )
                    {
                        return Failed("Supplier not found", ResultErrorType.Validation);
                    }

                    inventory.Quantity += receiveDto.Quantity;
                    inventory.AvailableQuantity += receiveDto.Quantity;
                    if (
                        inventory.Status == InventoryStatus.Assigned
                        && inventory.AvailableQuantity > 0
                    )
                    {
                        inventory.Status = InventoryStatus.Available;
                    }

                    await AddMovementAsync(
                        inventory,
                        StockMovementType.Received,
                        receiveDto.Quantity,
                        performedByUserId,
                        receiveDto.Reason,
                        receiveDto.Notes,
                        ct,
                        unitCost: receiveDto.UnitCost,
                        supplierId: receiveDto.SupplierId
                    );

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(inventory.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        return await FinishAsync(outcome, "Stock received successfully", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> AdjustStockAsync(
        int inventoryId,
        AdjustStockDto adjustDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (adjustDto.QuantityDelta == 0)
        {
            return Result<InventoryDto>.Validation("Adjustment quantity cannot be zero");
        }

        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork
                        .Inventories.GetByIdAsync(inventoryId, ct)
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found", ResultErrorType.NotFound);
                    }

                    if (inventory.Status == InventoryStatus.Disposed)
                    {
                        return Failed(
                            "Cannot adjust a disposed item",
                            ResultErrorType.Validation
                        );
                    }

                    var newAvailable = inventory.AvailableQuantity + adjustDto.QuantityDelta;
                    if (newAvailable < 0)
                    {
                        return Failed(
                            "Adjustment would make available quantity negative",
                            ResultErrorType.Validation
                        );
                    }

                    // Adjust total and available by the same delta so the assigned-out count
                    // (Quantity - AvailableQuantity) is preserved.
                    inventory.Quantity += adjustDto.QuantityDelta;
                    inventory.AvailableQuantity = newAvailable;
                    if (
                        inventory.Status == InventoryStatus.Assigned
                        && inventory.AvailableQuantity > 0
                    )
                    {
                        inventory.Status = InventoryStatus.Available;
                    }

                    await AddMovementAsync(
                        inventory,
                        StockMovementType.Adjusted,
                        adjustDto.QuantityDelta,
                        performedByUserId,
                        adjustDto.Reason,
                        adjustDto.Notes,
                        ct
                    );

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(inventory.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        return await FinishAsync(outcome, "Stock adjusted successfully", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> DisposeStockAsync(
        int inventoryId,
        DisposeStockDto disposeDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork
                        .Inventories.GetByIdAsync(inventoryId, ct)
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found", ResultErrorType.NotFound);
                    }

                    // Only available (unassigned) stock can be disposed; assigned units must be
                    // returned first so counts never go inconsistent.
                    if (disposeDto.Quantity > inventory.AvailableQuantity)
                    {
                        return Failed(
                            $"Cannot dispose {disposeDto.Quantity}; only {inventory.AvailableQuantity} available",
                            ResultErrorType.Validation
                        );
                    }

                    inventory.Quantity -= disposeDto.Quantity;
                    inventory.AvailableQuantity -= disposeDto.Quantity;
                    if (inventory.Quantity == 0)
                    {
                        inventory.Status = InventoryStatus.Disposed;
                    }

                    await AddMovementAsync(
                        inventory,
                        StockMovementType.Disposed,
                        -disposeDto.Quantity,
                        performedByUserId,
                        disposeDto.Reason,
                        disposeDto.Notes,
                        ct
                    );

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(inventory.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        return await FinishAsync(outcome, "Stock disposed successfully", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<InventoryDto>> TransferStockAsync(
        int inventoryId,
        TransferStockDto transferDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork
                        .Inventories.GetByIdAsync(inventoryId, ct)
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found", ResultErrorType.NotFound);
                    }

                    if (inventory.LocationId == transferDto.ToLocationId)
                    {
                        return Failed(
                            "Item is already in the target location",
                            ResultErrorType.Validation
                        );
                    }

                    var target = await _unitOfWork
                        .Locations.GetByIdAsync(transferDto.ToLocationId, ct)
                        .ConfigureAwait(false);
                    if (target is null)
                    {
                        return Failed("Target location not found", ResultErrorType.Validation);
                    }

                    var fromLocationId = inventory.LocationId;
                    inventory.LocationId = target.Id;
                    // Keep the legacy free-text location in sync for display/back-compat.
                    inventory.Location = target.Name;

                    await AddMovementAsync(
                        inventory,
                        StockMovementType.Transferred,
                        0,
                        performedByUserId,
                        transferDto.Reason,
                        transferDto.Notes,
                        ct,
                        fromLocationId: fromLocationId,
                        toLocationId: target.Id
                    );

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(inventory.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        return await FinishAsync(outcome, "Stock transferred successfully", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<StockMovementDto>>> GetMovementsAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    )
    {
        var exists = await _unitOfWork
            .Inventories.AnyAsync(x => x.Id == inventoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return Result<IEnumerable<StockMovementDto>>.NotFound("Inventory not found");
        }

        var movements = await _unitOfWork
            .StockMovements.GetByInventoryIdAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<StockMovementDto>>.Success(movements.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<StockMovementDto>>> GetRecentMovementsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .StockMovements.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
                include: q =>
                    q.Include(x => x.Inventory)
                        .Include(x => x.PerformedByUser)
                        .Include(x => x.FromLocation)
                        .Include(x => x.ToLocation)
                        .Include(x => x.Supplier),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<StockMovementDto>
        {
            Data = page.Items.ToDto(),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        return Result<PagedResult<StockMovementDto>>.Success(result);
    }

    /// <summary>Appends a ledger row for a movement. <c>BalanceAfter</c> snapshots available quantity.</summary>
    private Task AddMovementAsync(
        Inventory inventory,
        StockMovementType movementType,
        int quantityChange,
        int performedByUserId,
        string? reason,
        string? notes,
        CancellationToken cancellationToken,
        decimal? unitCost = null,
        int? supplierId = null,
        int? assignmentId = null,
        int? fromLocationId = null,
        int? toLocationId = null
    )
    {
        var movement = new StockMovement
        {
            InventoryId = inventory.Id,
            MovementType = movementType,
            QuantityChange = quantityChange,
            BalanceAfter = inventory.AvailableQuantity,
            Reason = reason,
            Notes = notes,
            UnitCost = unitCost,
            PerformedByUserId = performedByUserId,
            SupplierId = supplierId,
            AssignmentId = assignmentId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
        };
        return _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);
    }

    /// <summary>Maps a transactional outcome to a reloaded inventory DTO or a typed failure.</summary>
    private async Task<Result<InventoryDto>> FinishAsync(
        TransactionOutcome outcome,
        string successMessage,
        CancellationToken cancellationToken
    )
    {
        if (!outcome.Success)
        {
            return ToFailureResult<InventoryDto>(outcome);
        }

        _logger.LogInformation(
            "Stock operation completed for inventory {InventoryId}.",
            outcome.InventoryId
        );
        var reloaded = await _unitOfWork
            .Inventories.FirstOrDefaultAsync(
                x => x.Id == outcome.InventoryId,
                include: q =>
                    q.Include(i => i.CreatedByUser)
                        .Include(i => i.SupplierEntity)
                        .Include(i => i.LocationEntity),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return Result<InventoryDto>.Success(reloaded!.ToDto(), successMessage);
    }

    private static TransactionOutcome Failed(
        string error,
        ResultErrorType errorType = ResultErrorType.Failure
    ) => new(false, 0, error, errorType);

    private static TransactionOutcome Succeeded(int inventoryId) =>
        new(true, inventoryId, null, ResultErrorType.None);

    private static Result<T> ToFailureResult<T>(TransactionOutcome outcome) =>
        outcome.ErrorType switch
        {
            ResultErrorType.NotFound => Result<T>.NotFound(outcome.Error!),
            ResultErrorType.Conflict => Result<T>.Conflict(outcome.Error!),
            ResultErrorType.Validation => Result<T>.Validation(outcome.Error!),
            _ => Result<T>.Failure(outcome.Error!),
        };

    private readonly record struct TransactionOutcome(
        bool Success,
        int InventoryId,
        string? Error,
        ResultErrorType ErrorType
    );
}
