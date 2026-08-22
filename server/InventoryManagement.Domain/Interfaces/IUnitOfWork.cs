namespace InventoryManagement.Domain.Interfaces;

/// <summary>
/// Coordinates repositories over a single database session and commits their changes atomically.
/// Transaction management is encapsulated by <see cref="ExecuteInTransactionAsync{TResult}"/>
/// (which applies the provider's execution strategy for resiliency), so callers never handle
/// begin/commit/rollback directly. The underlying context is owned by the DI container, hence
/// this abstraction intentionally does not implement <see cref="IDisposable"/>.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>User repository bound to the shared session.</summary>
    IUserRepository Users { get; }

    /// <summary>Inventory repository bound to the shared session.</summary>
    IInventoryRepository Inventories { get; }

    /// <summary>Assignment repository bound to the shared session.</summary>
    IInventoryAssignmentRepository InventoryAssignments { get; }

    /// <summary>Stock-movement ledger repository bound to the shared session.</summary>
    IStockMovementRepository StockMovements { get; }

    /// <summary>Supplier repository bound to the shared session.</summary>
    ISupplierRepository Suppliers { get; }

    /// <summary>Location repository bound to the shared session.</summary>
    ILocationRepository Locations { get; }

    /// <summary>Maintenance-schedule repository bound to the shared session.</summary>
    IMaintenanceScheduleRepository MaintenanceSchedules { get; }

    /// <summary>Role repository bound to the shared session.</summary>
    IRoleRepository Roles { get; }

    /// <summary>Permission-catalog repository bound to the shared session.</summary>
    IPermissionRepository Permissions { get; }

    /// <summary>User-role assignment repository bound to the shared session.</summary>
    IUserRoleRepository UserRoles { get; }

    /// <summary>
    /// Persists all staged changes. Translates optimistic-concurrency failures into a
    /// <see cref="Exceptions.ConcurrencyConflictException"/> for consistent handling.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="operation"/> inside a database transaction using the provider's
    /// execution strategy (safe retries). Commits on success and rolls back on any exception,
    /// guaranteeing no partial writes and no leaked transaction/connection resources.
    /// </summary>
    /// <typeparam name="TResult">Result produced by the operation.</typeparam>
    /// <param name="operation">The transactional work, receiving a cancellation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default
    );
}
