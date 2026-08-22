using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="Supplier"/> aggregates.</summary>
public interface ISupplierRepository : IGenericRepository<Supplier>
{
    /// <summary>Returns whether a supplier with the given name already exists (case-insensitive).</summary>
    Task<bool> IsNameExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default
    );
}
