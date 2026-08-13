using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="InventoryAssignment"/> with allocation-oriented queries.</summary>
public class InventoryAssignmentRepository
    : GenericRepository<InventoryAssignment>,
        IInventoryAssignmentRepository
{
    /// <summary>Creates the repository.</summary>
    public InventoryAssignmentRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <summary>Standard read query: tracking-free with the common related graph eager-loaded.</summary>
    private IQueryable<InventoryAssignment> ReadQuery() =>
        EntitySet
            .AsNoTracking()
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        await ReadQuery()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    ) =>
        await ReadQuery()
            .Where(x => x.InventoryId == inventoryId)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetActiveAssignmentsAsync(
        CancellationToken cancellationToken = default
    ) =>
        await ReadQuery()
            .Where(x => x.Status == AssignmentStatus.Active)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetActiveAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        await ReadQuery()
            .Where(x => x.UserId == userId && x.Status == AssignmentStatus.Active)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByStatusAsync(
        AssignmentStatus status,
        CancellationToken cancellationToken = default
    ) =>
        await ReadQuery()
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetOverdueAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var today = DateTime.UtcNow.Date;
        return await ReadQuery()
            .Where(x =>
                x.Status == AssignmentStatus.Active
                && x.ExpectedReturnDate.HasValue
                && x.ExpectedReturnDate.Value.Date < today
            )
            .OrderBy(x => x.ExpectedReturnDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InventoryAssignment?> GetActiveAssignmentAsync(
        int inventoryId,
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.InventoryId == inventoryId
                    && x.UserId == userId
                    && x.Status == AssignmentStatus.Active,
                cancellationToken
            )
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> HasActiveAssignmentAsync(
        int inventoryId,
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AnyAsync(
                x =>
                    x.InventoryId == inventoryId
                    && x.UserId == userId
                    && x.Status == AssignmentStatus.Active,
                cancellationToken
            )
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryAssignment>> GetAssignmentHistoryAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.InventoryId == inventoryId)
            .Include(x => x.Inventory)
            .Include(x => x.User)
            .Include(x => x.AssignedByUser)
            .Include(x => x.ReturnedToUser)
            .OrderByDescending(x => x.AssignedDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
