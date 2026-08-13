using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>
/// Default <see cref="IUnitOfWork"/>. Repositories are injected (single DI wiring) and share the
/// scoped <see cref="AppDbContext"/>. Transactions are managed via the provider execution strategy
/// so partial writes and leaked transaction resources cannot occur. The context is owned by the
/// container, so this type intentionally does not dispose it.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    /// <summary>Creates the unit of work over the shared context and repositories.</summary>
    public UnitOfWork(
        AppDbContext dbContext,
        IUserRepository users,
        IInventoryRepository inventories,
        IInventoryAssignmentRepository inventoryAssignments
    )
    {
        _dbContext = dbContext;
        Users = users;
        Inventories = inventories;
        InventoryAssignments = inventoryAssignments;
    }

    /// <inheritdoc />
    public IUserRepository Users { get; }

    /// <inheritdoc />
    public IInventoryRepository Inventories { get; }

    /// <inheritdoc />
    public IInventoryAssignmentRepository InventoryAssignments { get; }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entityName = ex.Entries.Count > 0
                ? ex.Entries[0].Entity.GetType().Name
                : "record";
            throw new ConcurrencyConflictException(entityName);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy
            .ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext
                    .Database.BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false);
                try
                {
                    var result = await operation(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            })
            .ConfigureAwait(false);
    }
}
