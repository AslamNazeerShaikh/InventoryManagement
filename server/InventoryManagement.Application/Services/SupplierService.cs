using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>Supplier directory service. Standard CRUD with name-uniqueness and delete-guard rules.</summary>
public sealed class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SupplierService> _logger;

    /// <summary>Creates the supplier service.</summary>
    public SupplierService(IUnitOfWork unitOfWork, ILogger<SupplierService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<SupplierDto>>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default
    )
    {
        var suppliers = await _unitOfWork
            .Suppliers.ListAsync(
                predicate: activeOnly ? s => s.IsActive : null,
                orderBy: q => q.OrderBy(s => s.Name),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var counts = await _unitOfWork
            .Inventories.GetCountsBySupplierAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = suppliers.Select(s => s.ToDto(counts.GetValueOrDefault(s.Id)));
        return Result<IEnumerable<SupplierDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<SupplierDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .Suppliers.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderBy(s => s.Name),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var counts = await _unitOfWork
            .Inventories.GetCountsBySupplierAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new PagedResult<SupplierDto>
        {
            Data = page.Items.Select(s => s.ToDto(counts.GetValueOrDefault(s.Id))),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        return Result<PagedResult<SupplierDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var supplier = await _unitOfWork
            .Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return Result<SupplierDto>.NotFound("Supplier not found");
        }

        var count = await _unitOfWork
            .Inventories.CountAsync(x => x.SupplierId == id, cancellationToken)
            .ConfigureAwait(false);
        return Result<SupplierDto>.Success(supplier.ToDto(count));
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDto>> CreateAsync(
        CreateSupplierDto createDto,
        CancellationToken cancellationToken = default
    )
    {
        if (
            await _unitOfWork
                .Suppliers.IsNameExistsAsync(createDto.Name, cancellationToken: cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<SupplierDto>.Conflict("A supplier with that name already exists");
        }

        var supplier = createDto.ToEntity();
        await _unitOfWork.Suppliers.AddAsync(supplier, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created supplier {SupplierId}.", supplier.Id);
        return Result<SupplierDto>.Success(supplier.ToDto(), "Supplier created successfully");
    }

    /// <inheritdoc />
    public async Task<Result<SupplierDto>> UpdateAsync(
        int id,
        UpdateSupplierDto updateDto,
        CancellationToken cancellationToken = default
    )
    {
        var supplier = await _unitOfWork
            .Suppliers.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return Result<SupplierDto>.NotFound("Supplier not found");
        }

        if (
            !string.Equals(supplier.Name, updateDto.Name, StringComparison.Ordinal)
            && await _unitOfWork
                .Suppliers.IsNameExistsAsync(updateDto.Name, id, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<SupplierDto>.Conflict("A supplier with that name already exists");
        }

        updateDto.UpdateEntity(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated supplier {SupplierId}.", id);
        var count = await _unitOfWork
            .Inventories.CountAsync(x => x.SupplierId == id, cancellationToken)
            .ConfigureAwait(false);
        return Result<SupplierDto>.Success(supplier.ToDto(count), "Supplier updated successfully");
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var supplier = await _unitOfWork
            .Suppliers.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return Result<bool>.NotFound("Supplier not found");
        }

        var linkedItems = await _unitOfWork
            .Inventories.CountAsync(x => x.SupplierId == id, cancellationToken)
            .ConfigureAwait(false);
        if (linkedItems > 0)
        {
            return Result<bool>.Conflict(
                $"Cannot delete supplier with {linkedItems} linked item(s); reassign them first"
            );
        }

        supplier.IsDeleted = true;
        supplier.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted supplier {SupplierId}.", id);
        return Result<bool>.Success(true, "Supplier deleted successfully");
    }
}
