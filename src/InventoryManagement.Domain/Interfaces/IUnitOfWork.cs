namespace InventoryManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IInventoryRepository Inventories { get; }
    IInventoryAssignmentRepository InventoryAssignments { get; }

    Task<int> SaveAsync();
    Task<int> SaveAsync(CancellationToken cancellationToken);
    void Rollback();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
