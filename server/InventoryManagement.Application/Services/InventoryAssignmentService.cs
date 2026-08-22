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
/// Assignment service. Stock-changing operations run inside a transaction and rely on the entity
/// concurrency token to prevent oversell under concurrent requests; conflicts surface as HTTP 409.
/// </summary>
public sealed class InventoryAssignmentService : IInventoryAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryAssignmentService> _logger;

    /// <summary>Creates the assignment service.</summary>
    public InventoryAssignmentService(
        IUnitOfWork unitOfWork,
        ILogger<InventoryAssignmentService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.ListAsync(
                include: q =>
                    q.Include(x => x.Inventory).Include(x => x.User).Include(x => x.AssignedByUser),
                orderBy: q => q.OrderByDescending(x => x.AssignedDate),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAssignmentDto>> GetAssignmentByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var assignment = await _unitOfWork
            .InventoryAssignments.FirstOrDefaultAsync(
                x => x.Id == id,
                include: q =>
                    q.Include(a => a.Inventory)
                        .Include(a => a.User)
                        .Include(a => a.AssignedByUser)
                        .Include(a => a.ReturnedToUser),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return assignment is null
            ? Result<InventoryAssignmentDto>.NotFound("Assignment not found")
            : Result<InventoryAssignmentDto>.Success(assignment.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAssignmentDto>> CreateAssignmentAsync(
        CreateInventoryAssignmentDto createAssignmentDto,
        int assignedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork
                        .Inventories.GetByIdAsync(createAssignmentDto.InventoryId, ct)
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found", ResultErrorType.NotFound);
                    }

                    if (inventory.Status != InventoryStatus.Available)
                    {
                        return Failed(
                            "Inventory is not available for assignment",
                            ResultErrorType.Validation
                        );
                    }

                    if (
                        inventory.ExpiryDate.HasValue
                        && inventory.ExpiryDate.Value < DateTime.UtcNow
                    )
                    {
                        return Failed(
                            "Cannot assign expired inventory",
                            ResultErrorType.Validation
                        );
                    }

                    if (inventory.AvailableQuantity < createAssignmentDto.AssignedQuantity)
                    {
                        return Failed(
                            $"Insufficient quantity. Available: {inventory.AvailableQuantity}, "
                                + $"Requested: {createAssignmentDto.AssignedQuantity}",
                            ResultErrorType.Validation
                        );
                    }

                    var user = await _unitOfWork
                        .Users.GetByIdAsync(createAssignmentDto.UserId, ct)
                        .ConfigureAwait(false);
                    if (user is null)
                    {
                        return Failed("User not found", ResultErrorType.NotFound);
                    }

                    var assignment = createAssignmentDto.ToEntity();
                    assignment.AssignedByUserId = assignedByUserId;
                    assignment.Status = AssignmentStatus.Active;
                    await _unitOfWork
                        .InventoryAssignments.AddAsync(assignment, ct)
                        .ConfigureAwait(false);

                    inventory.AvailableQuantity -= createAssignmentDto.AssignedQuantity;
                    if (inventory.AvailableQuantity == 0)
                    {
                        inventory.Status = InventoryStatus.Assigned;
                    }

                    // Record the allocation in the audit ledger. The assignment navigation lets EF
                    // resolve the (identity) foreign key when both rows are inserted together.
                    var movement = new StockMovement
                    {
                        InventoryId = inventory.Id,
                        Assignment = assignment,
                        MovementType = StockMovementType.Assigned,
                        QuantityChange = -createAssignmentDto.AssignedQuantity,
                        BalanceAfter = inventory.AvailableQuantity,
                        PerformedByUserId = assignedByUserId,
                        Reason = "Assigned to recipient",
                        Notes = createAssignmentDto.AssignmentNotes,
                    };
                    await _unitOfWork.StockMovements.AddAsync(movement, ct).ConfigureAwait(false);

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ToFailureResult<InventoryAssignmentDto>(outcome);
        }

        _logger.LogInformation("Created assignment {AssignmentId}.", outcome.AssignmentId);
        var created = await LoadForDtoAsync(outcome.AssignmentId, cancellationToken)
            .ConfigureAwait(false);
        return Result<InventoryAssignmentDto>.Success(
            created!.ToDto(),
            "Assignment created successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAssignmentDto>> UpdateAssignmentAsync(
        int id,
        UpdateInventoryAssignmentDto updateAssignmentDto,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var assignment = await _unitOfWork
                        .InventoryAssignments.FirstOrDefaultAsync(
                            x => x.Id == id,
                            include: q => q.Include(a => a.Inventory),
                            asNoTracking: false,
                            cancellationToken: ct
                        )
                        .ConfigureAwait(false);

                    if (assignment is null)
                    {
                        return Failed("Assignment not found", ResultErrorType.NotFound);
                    }

                    if (assignment.Status != AssignmentStatus.Active)
                    {
                        return Failed(
                            "Can only update active assignments",
                            ResultErrorType.Validation
                        );
                    }

                    // Guard the stock invariant: terminal-status transitions (e.g. Returned) must go
                    // through the dedicated return flow so available quantity is reconciled. Allowing
                    // them here would flip the status without crediting stock back, corrupting counts.
                    if (updateAssignmentDto.Status != AssignmentStatus.Active)
                    {
                        return Failed(
                            "Assignment status cannot be changed via update; use the return endpoint to return an assignment.",
                            ResultErrorType.Validation
                        );
                    }

                    if (updateAssignmentDto.AssignedQuantity != assignment.AssignedQuantity)
                    {
                        var difference =
                            updateAssignmentDto.AssignedQuantity - assignment.AssignedQuantity;

                        if (difference > 0)
                        {
                            if (assignment.Inventory.AvailableQuantity < difference)
                            {
                                return Failed(
                                    "Insufficient inventory quantity available",
                                    ResultErrorType.Validation
                                );
                            }

                            assignment.Inventory.AvailableQuantity -= difference;
                            if (assignment.Inventory.AvailableQuantity == 0)
                            {
                                assignment.Inventory.Status = InventoryStatus.Assigned;
                            }
                        }
                        else
                        {
                            assignment.Inventory.AvailableQuantity += Math.Abs(difference);
                            if (
                                assignment.Inventory.Status == InventoryStatus.Assigned
                                && assignment.Inventory.AvailableQuantity > 0
                            )
                            {
                                assignment.Inventory.Status = InventoryStatus.Available;
                            }
                        }
                    }

                    updateAssignmentDto.UpdateEntity(assignment);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ToFailureResult<InventoryAssignmentDto>(outcome);
        }

        _logger.LogInformation("Updated assignment {AssignmentId}.", id);
        var updated = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<InventoryAssignmentDto>.Success(
            updated!.ToDto(),
            "Assignment updated successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ReturnAssignmentAsync(
        ReturnInventoryAssignmentDto returnAssignmentDto,
        int returnedToUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var assignment = await _unitOfWork
                        .InventoryAssignments.FirstOrDefaultAsync(
                            x => x.Id == returnAssignmentDto.AssignmentId,
                            include: q => q.Include(a => a.Inventory),
                            asNoTracking: false,
                            cancellationToken: ct
                        )
                        .ConfigureAwait(false);

                    if (assignment is null)
                    {
                        return Failed("Assignment not found", ResultErrorType.NotFound);
                    }

                    if (assignment.Status != AssignmentStatus.Active)
                    {
                        return Failed("Assignment is not active", ResultErrorType.Validation);
                    }

                    var outstanding = assignment.AssignedQuantity - assignment.ReturnedQuantity;
                    var returnQty = returnAssignmentDto.ReturnQuantity ?? outstanding;
                    if (returnQty < 1 || returnQty > outstanding)
                    {
                        return Failed(
                            $"Return quantity must be between 1 and {outstanding}",
                            ResultErrorType.Validation
                        );
                    }

                    var condition = returnAssignmentDto.ReturnCondition ?? ReturnCondition.Good;
                    var inventory = assignment.Inventory;

                    if (condition == ReturnCondition.Lost)
                    {
                        // Lost units never came back: remove them from the owned total instead of
                        // crediting available stock (keeps the AvailableQuantity <= Quantity invariant).
                        inventory.Quantity = Math.Max(0, inventory.Quantity - returnQty);
                    }
                    else
                    {
                        // Physically returned: credit available stock, capped at the owned total.
                        inventory.AvailableQuantity = Math.Min(
                            inventory.Quantity,
                            inventory.AvailableQuantity + returnQty
                        );
                    }

                    assignment.ReturnedQuantity += returnQty;
                    assignment.ReturnedToUserId = returnedToUserId;
                    assignment.ReturnNotes = returnAssignmentDto.ReturnNotes;
                    assignment.ReturnCondition = condition;

                    // A partial return leaves the assignment Active with the remainder still out.
                    if (assignment.ReturnedQuantity >= assignment.AssignedQuantity)
                    {
                        assignment.ReturnDate = DateTime.UtcNow;
                        assignment.Status = condition switch
                        {
                            ReturnCondition.Lost => AssignmentStatus.Lost,
                            ReturnCondition.Damaged => AssignmentStatus.Damaged,
                            _ => AssignmentStatus.Returned,
                        };
                    }

                    // Only a fully-assigned item returns to Available; never override
                    // Damaged/Expired/Disposed lifecycle states.
                    if (
                        inventory.Status == InventoryStatus.Assigned
                        && inventory.AvailableQuantity > 0
                    )
                    {
                        inventory.Status = InventoryStatus.Available;
                    }

                    // Ledger: a physical return credits stock (Returned); a lost item leaves
                    // circulation (Disposed) so the timeline reflects the true stock effect.
                    var movement = new StockMovement
                    {
                        InventoryId = inventory.Id,
                        AssignmentId = assignment.Id,
                        MovementType =
                            condition == ReturnCondition.Lost
                                ? StockMovementType.Disposed
                                : StockMovementType.Returned,
                        QuantityChange = condition == ReturnCondition.Lost ? -returnQty : returnQty,
                        BalanceAfter = inventory.AvailableQuantity,
                        PerformedByUserId = returnedToUserId,
                        Reason =
                            condition == ReturnCondition.Lost
                                ? "Reported lost on return"
                                : $"Returned ({condition})",
                        Notes = returnAssignmentDto.ReturnNotes,
                    };
                    await _unitOfWork.StockMovements.AddAsync(movement, ct).ConfigureAwait(false);

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ToFailureResult<bool>(outcome);
        }

        _logger.LogInformation(
            "Returned assignment {AssignmentId}.",
            returnAssignmentDto.AssignmentId
        );
        return Result<bool>.Success(true, "Assignment returned successfully");
    }

    /// <inheritdoc />
    public async Task<Result<InventoryAssignmentDto>> RenewAssignmentAsync(
        RenewInventoryAssignmentDto renewAssignmentDto,
        CancellationToken cancellationToken = default
    )
    {
        var assignment = await _unitOfWork
            .InventoryAssignments.FirstOrDefaultAsync(
                x => x.Id == renewAssignmentDto.AssignmentId,
                asNoTracking: false,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (assignment is null)
        {
            return Result<InventoryAssignmentDto>.NotFound("Assignment not found");
        }

        if (assignment.Status != AssignmentStatus.Active)
        {
            return Result<InventoryAssignmentDto>.Validation("Can only renew active assignments");
        }

        if (renewAssignmentDto.NewExpectedReturnDate <= DateTime.UtcNow)
        {
            return Result<InventoryAssignmentDto>.Validation(
                "The new expected return date must be in the future"
            );
        }

        assignment.ExpectedReturnDate = renewAssignmentDto.NewExpectedReturnDate;
        assignment.RenewalCount += 1;
        if (!string.IsNullOrWhiteSpace(renewAssignmentDto.Notes))
        {
            assignment.AssignmentNotes = string.IsNullOrWhiteSpace(assignment.AssignmentNotes)
                ? renewAssignmentDto.Notes
                : $"{assignment.AssignmentNotes} | Renewed: {renewAssignmentDto.Notes}";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Renewed assignment {AssignmentId}.", assignment.Id);
        var updated = await LoadForDtoAsync(assignment.Id, cancellationToken).ConfigureAwait(false);
        return Result<InventoryAssignmentDto>.Success(
            updated!.ToDto(),
            "Assignment renewed successfully"
        );
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryAssignmentDto>>> GetDueSoonAssignmentsAsync(
        int daysAhead = 7,
        CancellationToken cancellationToken = default
    )
    {
        daysAhead = Math.Clamp(daysAhead, 0, BusinessConstants.Assignment.MaxAssignmentDays);
        var now = DateTime.UtcNow;
        var until = now.AddDays(daysAhead);

        // Active, not yet overdue, and due within the window — all filtered SQL-side.
        var assignments = await _unitOfWork
            .InventoryAssignments.ListAsync(
                predicate: x =>
                    x.Status == AssignmentStatus.Active
                    && x.ExpectedReturnDate != null
                    && x.ExpectedReturnDate >= now
                    && x.ExpectedReturnDate <= until,
                include: q =>
                    q.Include(x => x.Inventory).Include(x => x.User).Include(x => x.AssignedByUser),
                orderBy: q => q.OrderBy(x => x.ExpectedReturnDate),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryAssignmentDto>>> GetAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetAssignmentsByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetActiveAssignmentsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<
        Result<IEnumerable<InventoryAssignmentDto>>
    > GetActiveAssignmentsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetActiveAssignmentsByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetOverdueAssignmentsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<AssignmentHistoryDto>> GetAssignmentHistoryAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await _unitOfWork
            .Inventories.GetByIdAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);
        if (inventory is null)
        {
            return Result<AssignmentHistoryDto>.NotFound("Inventory not found");
        }

        var assignments = await _unitOfWork
            .InventoryAssignments.GetAssignmentHistoryAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);

        var historyDto = new AssignmentHistoryDto
        {
            InventoryId = inventoryId,
            ItemName = inventory.Name,
            Assignments = assignments.ToDto().ToList(),
        };

        return Result<AssignmentHistoryDto>.Success(historyDto);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<InventoryAssignmentDto>>> GetAssignmentsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        // Include the related graph so DTO projection has all navigation data (fixes prior NRE).
        var page = await _unitOfWork
            .InventoryAssignments.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderByDescending(x => x.AssignedDate),
                include: q =>
                    q.Include(x => x.Inventory).Include(x => x.User).Include(x => x.AssignedByUser),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<InventoryAssignmentDto>
        {
            Data = page.Items.ToDto(),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return Result<PagedResult<InventoryAssignmentDto>>.Success(result);
    }

    /// <summary>Loads a single assignment (tracking-free) with its related graph for projection.</summary>
    private Task<InventoryAssignment?> LoadForDtoAsync(
        int id,
        CancellationToken cancellationToken
    ) =>
        _unitOfWork.InventoryAssignments.FirstOrDefaultAsync(
            x => x.Id == id,
            include: q =>
                q.Include(a => a.Inventory).Include(a => a.User).Include(a => a.AssignedByUser),
            cancellationToken: cancellationToken
        );

    // Small transactional result helpers keep the delegate bodies readable and allocation-light.
    private static TransactionOutcome Failed(
        string error,
        ResultErrorType errorType = ResultErrorType.Failure
    ) => new(false, 0, error, errorType);

    private static TransactionOutcome Succeeded(int assignmentId) =>
        new(true, assignmentId, null, ResultErrorType.None);

    /// <summary>Maps a failed transactional outcome onto the correctly-typed <see cref="Result{T}"/>.</summary>
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
        int AssignmentId,
        string? Error,
        ResultErrorType ErrorType
    );
}
