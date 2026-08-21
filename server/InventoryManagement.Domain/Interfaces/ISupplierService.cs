using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Managed supplier/vendor directory operations (CRUD + listing).</summary>
public interface ISupplierService
{
    /// <summary>Lists suppliers, optionally only active ones, with item counts.</summary>
    Task<Result<IEnumerable<SupplierDto>>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of suppliers with item counts.</summary>
    Task<Result<PagedResult<SupplierDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets a supplier by identifier.</summary>
    Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates a supplier.</summary>
    Task<Result<SupplierDto>> CreateAsync(
        CreateSupplierDto createDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates a supplier.</summary>
    Task<Result<SupplierDto>> UpdateAsync(
        int id,
        UpdateSupplierDto updateDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes a supplier that has no linked inventory items.</summary>
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
