using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Managed storage-location operations (CRUD + listing).</summary>
public interface ILocationService
{
    /// <summary>Lists locations, optionally only active ones, with item counts.</summary>
    Task<Result<IEnumerable<LocationDto>>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of locations with item counts.</summary>
    Task<Result<PagedResult<LocationDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets a location by identifier.</summary>
    Task<Result<LocationDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a location.</summary>
    Task<Result<LocationDto>> CreateAsync(
        CreateLocationDto createDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates a location.</summary>
    Task<Result<LocationDto>> UpdateAsync(
        int id,
        UpdateLocationDto updateDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes a location that has no child locations and no linked inventory items.</summary>
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
