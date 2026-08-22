using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for the tenant's <see cref="Permission"/> catalog.</summary>
public interface IPermissionRepository : IGenericRepository<Permission>
{
    /// <summary>Lists the tenant's permission catalog ordered by category then code.</summary>
    Task<IReadOnlyList<Permission>> ListCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the permissions whose codes are contained in <paramref name="codes"/>.</summary>
    Task<IReadOnlyList<Permission>> GetByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default
    );
}
