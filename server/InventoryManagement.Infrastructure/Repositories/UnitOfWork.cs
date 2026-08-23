using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Data.Providers;
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
    private readonly IDatabaseProviderDialect _dialect;

    /// <summary>Creates the unit of work over the shared context and repositories.</summary>
    public UnitOfWork(
        AppDbContext dbContext,
        IUserRepository users,
        IInventoryRepository inventories,
        IInventoryAssignmentRepository inventoryAssignments,
        IStockMovementRepository stockMovements,
        ISupplierRepository suppliers,
        ILocationRepository locations,
        IMaintenanceScheduleRepository maintenanceSchedules,
        IRoleRepository roles,
        IPermissionRepository permissions,
        IUserRoleRepository userRoles
    )
    {
        _dbContext = dbContext;
        // Resolved from the configured provider (not injected) so every context — runtime, seeding,
        // design-time and tests — translates store errors with the dialect that actually produced them.
        _dialect = DatabaseProviderDialects.For(dbContext.Database.ProviderName);
        Users = users;
        Inventories = inventories;
        InventoryAssignments = inventoryAssignments;
        StockMovements = stockMovements;
        Suppliers = suppliers;
        Locations = locations;
        MaintenanceSchedules = maintenanceSchedules;
        Roles = roles;
        Permissions = permissions;
        UserRoles = userRoles;
    }

    /// <inheritdoc />
    public IUserRepository Users { get; }

    /// <inheritdoc />
    public IInventoryRepository Inventories { get; }

    /// <inheritdoc />
    public IInventoryAssignmentRepository InventoryAssignments { get; }

    /// <inheritdoc />
    public IStockMovementRepository StockMovements { get; }

    /// <inheritdoc />
    public ISupplierRepository Suppliers { get; }

    /// <inheritdoc />
    public ILocationRepository Locations { get; }

    /// <inheritdoc />
    public IMaintenanceScheduleRepository MaintenanceSchedules { get; }

    /// <inheritdoc />
    public IRoleRepository Roles { get; }

    /// <inheritdoc />
    public IPermissionRepository Permissions { get; }

    /// <inheritdoc />
    public IUserRoleRepository UserRoles { get; }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ResolveEntityName(ex));
        }
        catch (DbUpdateException ex) when (_dialect.IsUniqueConstraintViolation(ex))
        {
            // A concurrent writer inserted the same unique value after the application-level
            // pre-check passed (check-then-act). The store correctly rejected the duplicate, so this
            // is a conflict — not a server fault — and must surface as 409 rather than 500. Any
            // other DbUpdateException (foreign key, NOT NULL, …) is left untouched.
            throw new DuplicateEntityException(ResolveEntityName(ex), ex);
        }
    }

    /// <summary>Names the entity a failed save was attempting to write, for the error message.</summary>
    private static string ResolveEntityName(DbUpdateException exception) =>
        exception.Entries.Count > 0 ? exception.Entries[0].Entity.GetType().Name : "record";

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
