using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="Location"/> aggregates (self-referencing hierarchy).</summary>
public interface ILocationRepository : IGenericRepository<Location>
{
    /// <summary>Returns whether a location with the given code already exists (case-insensitive).</summary>
    Task<bool> IsCodeExistsAsync(
        string code,
        int? excludeId = null,
        CancellationToken cancellationToken = default
    );
}
