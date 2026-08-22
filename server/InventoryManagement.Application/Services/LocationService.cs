using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Location directory service. Standard CRUD plus hierarchy rules: unique codes, parent existence,
/// cycle prevention, and delete guards (no children, no linked items).
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LocationService> _logger;

    /// <summary>Creates the location service.</summary>
    public LocationService(IUnitOfWork unitOfWork, ILogger<LocationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<LocationDto>>> GetAllAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default
    )
    {
        var locations = await _unitOfWork
            .Locations.ListAsync(
                predicate: activeOnly ? l => l.IsActive : null,
                include: q => q.Include(l => l.ParentLocation),
                orderBy: q => q.OrderBy(l => l.Name),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var counts = await _unitOfWork
            .Inventories.GetCountsByLocationAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = locations.Select(l => l.ToDto(counts.GetValueOrDefault(l.Id)));
        return Result<IEnumerable<LocationDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<LocationDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .Locations.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderBy(l => l.Name),
                include: q => q.Include(l => l.ParentLocation),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var counts = await _unitOfWork
            .Inventories.GetCountsByLocationAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new PagedResult<LocationDto>
        {
            Data = page.Items.Select(l => l.ToDto(counts.GetValueOrDefault(l.Id))),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        return Result<PagedResult<LocationDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<LocationDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var location = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        if (location is null)
        {
            return Result<LocationDto>.NotFound("Location not found");
        }

        var count = await _unitOfWork
            .Inventories.CountAsync(x => x.LocationId == id, cancellationToken)
            .ConfigureAwait(false);
        return Result<LocationDto>.Success(location.ToDto(count));
    }

    /// <inheritdoc />
    public async Task<Result<LocationDto>> CreateAsync(
        CreateLocationDto createDto,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !string.IsNullOrWhiteSpace(createDto.Code)
            && await _unitOfWork
                .Locations.IsCodeExistsAsync(createDto.Code, cancellationToken: cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<LocationDto>.Conflict("A location with that code already exists");
        }

        if (createDto.ParentLocationId is int parentId)
        {
            var parentExists = await _unitOfWork
                .Locations.AnyAsync(l => l.Id == parentId, cancellationToken)
                .ConfigureAwait(false);
            if (!parentExists)
            {
                return Result<LocationDto>.Validation("Parent location not found");
            }
        }

        var location = createDto.ToEntity();
        await _unitOfWork.Locations.AddAsync(location, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created location {LocationId}.", location.Id);
        var reloaded = await LoadForDtoAsync(location.Id, cancellationToken).ConfigureAwait(false);
        return Result<LocationDto>.Success(reloaded!.ToDto(), "Location created successfully");
    }

    /// <inheritdoc />
    public async Task<Result<LocationDto>> UpdateAsync(
        int id,
        UpdateLocationDto updateDto,
        CancellationToken cancellationToken = default
    )
    {
        var location = await _unitOfWork
            .Locations.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (location is null)
        {
            return Result<LocationDto>.NotFound("Location not found");
        }

        if (
            !string.IsNullOrWhiteSpace(updateDto.Code)
            && !string.Equals(location.Code, updateDto.Code, StringComparison.Ordinal)
            && await _unitOfWork
                .Locations.IsCodeExistsAsync(updateDto.Code, id, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<LocationDto>.Conflict("A location with that code already exists");
        }

        if (updateDto.ParentLocationId is int parentId)
        {
            if (parentId == id)
            {
                return Result<LocationDto>.Validation("A location cannot be its own parent");
            }

            var cycleCheck = await WouldCreateCycleAsync(id, parentId, cancellationToken)
                .ConfigureAwait(false);
            if (cycleCheck)
            {
                return Result<LocationDto>.Validation(
                    "The chosen parent would create a location cycle"
                );
            }
        }

        updateDto.UpdateEntity(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated location {LocationId}.", id);
        var reloaded = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        var count = await _unitOfWork
            .Inventories.CountAsync(x => x.LocationId == id, cancellationToken)
            .ConfigureAwait(false);
        return Result<LocationDto>.Success(reloaded!.ToDto(count), "Location updated successfully");
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var location = await _unitOfWork
            .Locations.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (location is null)
        {
            return Result<bool>.NotFound("Location not found");
        }

        var childCount = await _unitOfWork
            .Locations.CountAsync(l => l.ParentLocationId == id, cancellationToken)
            .ConfigureAwait(false);
        if (childCount > 0)
        {
            return Result<bool>.Conflict(
                $"Cannot delete location with {childCount} child location(s)"
            );
        }

        var linkedItems = await _unitOfWork
            .Inventories.CountAsync(x => x.LocationId == id, cancellationToken)
            .ConfigureAwait(false);
        if (linkedItems > 0)
        {
            return Result<bool>.Conflict(
                $"Cannot delete location with {linkedItems} linked item(s); move them first"
            );
        }

        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted location {LocationId}.", id);
        return Result<bool>.Success(true, "Location deleted successfully");
    }

    /// <summary>Loads a location (tracking-free) with its parent navigation for projection.</summary>
    private Task<Location?> LoadForDtoAsync(int id, CancellationToken cancellationToken) =>
        _unitOfWork.Locations.FirstOrDefaultAsync(
            l => l.Id == id,
            include: q => q.Include(x => x.ParentLocation),
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Returns whether re-parenting <paramref name="locationId"/> under <paramref name="newParentId"/>
    /// would create a cycle, by walking the ancestor chain of the proposed parent.
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(
        int locationId,
        int newParentId,
        CancellationToken cancellationToken
    )
    {
        int? currentId = newParentId;
        var guard = 0;
        while (currentId is int cid && guard++ < 1000)
        {
            if (cid == locationId)
            {
                return true;
            }

            var parent = await _unitOfWork
                .Locations.FirstOrDefaultAsync(l => l.Id == cid, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            currentId = parent?.ParentLocationId;
        }

        return false;
    }
}
