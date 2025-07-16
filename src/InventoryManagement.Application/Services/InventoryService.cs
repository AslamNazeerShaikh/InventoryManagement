using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetAllInventoriesAsync()
    {
        try
        {
            var inventories = await _unitOfWork.Inventories.GetAllAsync(x => x.CreatedByUser);
            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<InventoryDto>> GetInventoryByIdAsync(int id)
    {
        try
        {
            var inventory = await _unitOfWork.Inventories.GetByIdAsync(id, x => x.CreatedByUser);
            if (inventory == null)
            {
                return ApiResponse<InventoryDto>.Failure("Inventory not found");
            }

            return ApiResponse<InventoryDto>.Success(inventory.ToDto());
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryDto>.Failure($"Error retrieving inventory: {ex.Message}");
        }
    }

    public async Task<ApiResponse<InventoryDto>> GetInventoryByBarcodeAsync(string barcode)
    {
        try
        {
            var inventory = await _unitOfWork.Inventories.GetByBarcodeAsync(barcode);
            if (inventory == null)
            {
                return ApiResponse<InventoryDto>.Failure("Inventory not found");
            }

            return ApiResponse<InventoryDto>.Success(inventory.ToDto());
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryDto>.Failure($"Error retrieving inventory: {ex.Message}");
        }
    }

    public async Task<ApiResponse<InventoryDto>> CreateInventoryAsync(
        CreateInventoryDto createInventoryDto,
        int createdByUserId
    )
    {
        try
        {
            // Validate barcode uniqueness if provided
            if (
                !string.IsNullOrEmpty(createInventoryDto.Barcode)
                && await _unitOfWork.Inventories.IsBarcodeExistsAsync(createInventoryDto.Barcode)
            )
            {
                return ApiResponse<InventoryDto>.Failure("Barcode already exists");
            }

            // Validate serial number uniqueness if provided
            if (
                !string.IsNullOrEmpty(createInventoryDto.SerialNumber)
                && await _unitOfWork.Inventories.IsSerialNumberExistsAsync(
                    createInventoryDto.SerialNumber
                )
            )
            {
                return ApiResponse<InventoryDto>.Failure("Serial number already exists");
            }

            // Create inventory entity
            var inventory = createInventoryDto.ToEntity();
            inventory.CreatedByUserId = createdByUserId;
            inventory.Status = InventoryStatus.Available;

            // Add inventory
            await _unitOfWork.Inventories.AddAsync(inventory);
            await _unitOfWork.SaveAsync();

            // Get the created inventory with user details
            var createdInventory = await _unitOfWork.Inventories.GetByIdAsync(
                inventory.Id,
                x => x.CreatedByUser
            );
            return ApiResponse<InventoryDto>.Success(
                createdInventory!.ToDto(),
                "Inventory created successfully"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryDto>.Failure($"Error creating inventory: {ex.Message}");
        }
    }

    public async Task<ApiResponse<InventoryDto>> UpdateInventoryAsync(
        int id,
        UpdateInventoryDto updateInventoryDto
    )
    {
        try
        {
            var inventory = await _unitOfWork.Inventories.GetByIdAsync(id);
            if (inventory == null)
            {
                return ApiResponse<InventoryDto>.Failure("Inventory not found");
            }

            // Validate barcode uniqueness if being changed
            if (
                !string.IsNullOrEmpty(updateInventoryDto.Barcode)
                && inventory.Barcode != updateInventoryDto.Barcode
                && await _unitOfWork.Inventories.IsBarcodeExistsAsync(updateInventoryDto.Barcode)
            )
            {
                return ApiResponse<InventoryDto>.Failure("Barcode already exists");
            }

            // Validate serial number uniqueness if being changed
            if (
                !string.IsNullOrEmpty(updateInventoryDto.SerialNumber)
                && inventory.SerialNumber != updateInventoryDto.SerialNumber
                && await _unitOfWork.Inventories.IsSerialNumberExistsAsync(
                    updateInventoryDto.SerialNumber
                )
            )
            {
                return ApiResponse<InventoryDto>.Failure("Serial number already exists");
            }

            // Calculate available quantity if total quantity is being updated
            var quantityDifference = updateInventoryDto.Quantity - inventory.Quantity;
            var newAvailableQuantity = inventory.AvailableQuantity + quantityDifference;

            // Ensure available quantity is not negative
            if (newAvailableQuantity < 0)
            {
                return ApiResponse<InventoryDto>.Failure(
                    "Cannot reduce quantity below assigned amount"
                );
            }

            // Update inventory
            updateInventoryDto.UpdateEntity(inventory);
            inventory.AvailableQuantity = newAvailableQuantity;

            _unitOfWork.Inventories.Update(inventory);
            await _unitOfWork.SaveAsync();

            // Get updated inventory with user details
            var updatedInventory = await _unitOfWork.Inventories.GetByIdAsync(
                id,
                x => x.CreatedByUser
            );
            return ApiResponse<InventoryDto>.Success(
                updatedInventory!.ToDto(),
                "Inventory updated successfully"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryDto>.Failure($"Error updating inventory: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteInventoryAsync(int id)
    {
        try
        {
            var inventory = await _unitOfWork.Inventories.GetByIdAsync(id);
            if (inventory == null)
            {
                return ApiResponse<bool>.Failure("Inventory not found");
            }

            // Check if inventory has active assignments
            if (
                await _unitOfWork.InventoryAssignments.AnyAsync(x =>
                    x.InventoryId == id && x.Status == AssignmentStatus.Active
                )
            )
            {
                return ApiResponse<bool>.Failure("Cannot delete inventory with active assignments");
            }

            // Soft delete
            inventory.IsDeleted = true;
            inventory.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Inventories.Update(inventory);
            await _unitOfWork.SaveAsync();

            return ApiResponse<bool>.Success(true, "Inventory deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Failure($"Error deleting inventory: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetAvailableInventoriesAsync()
    {
        try
        {
            var inventories = await _unitOfWork.Inventories.GetAvailableInventoriesAsync();
            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving available inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiringInventoriesAsync(
        int monthsBefore = 3
    )
    {
        try
        {
            var expiryDate = DateTime.UtcNow.AddMonths(monthsBefore);
            var inventories = await _unitOfWork.Inventories.GetExpiringInventoriesAsync(expiryDate);
            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving expiring inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockInventoriesAsync(
        int threshold = 5
    )
    {
        try
        {
            var inventories = await _unitOfWork.Inventories.GetLowStockInventoriesAsync(threshold);
            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving low stock inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> SearchInventoriesAsync(
        InventorySearchDto searchDto
    )
    {
        try
        {
            IEnumerable<Domain.Entities.Inventory> inventories;

            if (!string.IsNullOrEmpty(searchDto.SearchTerm))
            {
                inventories = await _unitOfWork.Inventories.SearchInventoriesAsync(
                    searchDto.SearchTerm
                );
            }
            else if (!string.IsNullOrEmpty(searchDto.Category))
            {
                inventories = await _unitOfWork.Inventories.GetInventoriesByCategoryAsync(
                    searchDto.Category
                );
            }
            else if (searchDto.Status.HasValue)
            {
                inventories = await _unitOfWork.Inventories.GetInventoriesByStatusAsync(
                    searchDto.Status.Value
                );
            }
            else
            {
                inventories = await _unitOfWork.Inventories.GetAllAsync(x => x.CreatedByUser);
            }

            // Apply additional filters
            if (searchDto.ExpiryDateFrom.HasValue || searchDto.ExpiryDateTo.HasValue)
            {
                inventories = inventories.Where(x =>
                    (
                        !searchDto.ExpiryDateFrom.HasValue
                        || (x.ExpiryDate.HasValue && x.ExpiryDate >= searchDto.ExpiryDateFrom)
                    )
                    && (
                        !searchDto.ExpiryDateTo.HasValue
                        || (x.ExpiryDate.HasValue && x.ExpiryDate <= searchDto.ExpiryDateTo)
                    )
                );
            }

            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error searching inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<PagedResult<InventoryDto>>> GetInventoriesPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        try
        {
            pageSize = Math.Min(pageSize, BusinessConstants.Pagination.MaxPageSize);

            var inventories = await _unitOfWork.Inventories.GetPagedAsync(pageNumber, pageSize);
            var totalCount = await _unitOfWork.Inventories.CountAsync();

            var pagedResult = new PagedResult<InventoryDto>
            {
                Data = inventories.ToDto(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            return ApiResponse<PagedResult<InventoryDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<InventoryDto>>.Failure(
                $"Error retrieving paged inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetInventoriesByCategoryAsync(
        string category
    )
    {
        try
        {
            var inventories = await _unitOfWork.Inventories.GetInventoriesByCategoryAsync(category);
            var inventoryDtos = inventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving inventories by category: {ex.Message}"
            );
        }
    }
}
