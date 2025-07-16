using System.Security.Claims;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger
    )
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory items
    /// </summary>
    /// <returns>List of all inventory items</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetAllInventories()
    {
        try
        {
            _logger.LogInformation("Requesting all inventory items");
            var result = await _inventoryService.GetAllInventoriesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get inventory items with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of inventory items</returns>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryDto>>>> GetInventoriesPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10
    )
    {
        try
        {
            if (pageSize > BusinessConstants.Pagination.MaxPageSize)
                pageSize = BusinessConstants.Pagination.MaxPageSize;

            _logger.LogInformation(
                "Requesting inventory items page {PageNumber} with size {PageSize}",
                pageNumber,
                pageSize
            );
            var result = await _inventoryService.GetInventoriesPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged inventory items");
            return StatusCode(
                500,
                ApiResponse<PagedResult<InventoryDto>>.Failure(
                    "An error occurred while retrieving inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get inventory item by ID
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <returns>Inventory item details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetInventoryById(int id)
    {
        try
        {
            _logger.LogInformation("Requesting inventory item {InventoryId}", id);
            var result = await _inventoryService.GetInventoryByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item {InventoryId}", id);
            return StatusCode(
                500,
                ApiResponse<InventoryDto>.Failure(
                    "An error occurred while retrieving inventory item"
                )
            );
        }
    }

    /// <summary>
    /// Get inventory item by barcode (for scanning functionality)
    /// </summary>
    /// <param name="barcode">Barcode value</param>
    /// <returns>Inventory item details</returns>
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetInventoryByBarcode(string barcode)
    {
        try
        {
            _logger.LogInformation("Requesting inventory item by barcode: {Barcode}", barcode);
            var result = await _inventoryService.GetInventoryByBarcodeAsync(barcode);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item by barcode: {Barcode}", barcode);
            return StatusCode(
                500,
                ApiResponse<InventoryDto>.Failure(
                    "An error occurred while retrieving inventory item"
                )
            );
        }
    }

    /// <summary>
    /// Search inventory items with advanced filters
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Filtered inventory items</returns>
    [HttpPost("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> SearchInventories(
        [FromBody] InventorySearchDto searchDto
    )
    {
        try
        {
            _logger.LogInformation(
                "Searching inventory items with criteria: {SearchTerm}",
                searchDto.SearchTerm
            );
            var result = await _inventoryService.SearchInventoriesAsync(searchDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while searching inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get available inventory items
    /// </summary>
    /// <returns>List of available inventory items</returns>
    [HttpGet("available")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryDto>>>
    > GetAvailableInventories()
    {
        try
        {
            _logger.LogInformation("Requesting available inventory items");
            var result = await _inventoryService.GetAvailableInventoriesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving available inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get inventory items expiring within specified months
    /// </summary>
    /// <param name="monthsBefore">Months before expiry (default: 3)</param>
    /// <returns>List of expiring inventory items</returns>
    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetExpiringInventories(
        [FromQuery] int monthsBefore = 3
    )
    {
        try
        {
            if (monthsBefore < BusinessConstants.ExpiryAlert.MinMonthsBefore)
                monthsBefore = BusinessConstants.ExpiryAlert.MinMonthsBefore;
            if (monthsBefore > BusinessConstants.ExpiryAlert.MaxMonthsBefore)
                monthsBefore = BusinessConstants.ExpiryAlert.MaxMonthsBefore;

            _logger.LogInformation(
                "Requesting inventory items expiring within {MonthsBefore} months",
                monthsBefore
            );
            var result = await _inventoryService.GetExpiringInventoriesAsync(monthsBefore);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving expiring inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get low stock inventory items
    /// </summary>
    /// <param name="threshold">Stock threshold (default: 5)</param>
    /// <returns>List of low stock inventory items</returns>
    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetLowStockInventories(
        [FromQuery] int threshold = BusinessConstants.Inventory.LowStockThreshold
    )
    {
        try
        {
            _logger.LogInformation(
                "Requesting low stock inventory items with threshold {Threshold}",
                threshold
            );
            var result = await _inventoryService.GetLowStockInventoriesAsync(threshold);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving low stock inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get inventory items by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of inventory items in the category</returns>
    [HttpGet("category/{category}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryDto>>>
    > GetInventoriesByCategory(string category)
    {
        try
        {
            _logger.LogInformation("Requesting inventory items in category: {Category}", category);
            var result = await _inventoryService.GetInventoriesByCategoryAsync(category);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving inventory items by category: {Category}",
                category
            );
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving inventory items by category"
                )
            );
        }
    }

    /// <summary>
    /// Create new inventory item (Admin or Provider)
    /// </summary>
    /// <param name="createInventoryDto">Inventory creation data</param>
    /// <returns>Created inventory item details</returns>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> CreateInventory(
        [FromBody] CreateInventoryDto createInventoryDto
    )
    {
        try
        {
            var userIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(ApiResponse<InventoryDto>.Failure("Invalid user ID"));
            }

            _logger.LogInformation(
                "User {UserId} creating inventory item: {EquipmentName}",
                userId,
                createInventoryDto.EquipmentName
            );

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<InventoryDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _inventoryService.CreateInventoryAsync(createInventoryDto, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Successfully created inventory item: {EquipmentName}",
                createInventoryDto.EquipmentName
            );
            return CreatedAtAction(nameof(GetInventoryById), new { id = result.Data!.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating inventory item: {EquipmentName}",
                createInventoryDto.EquipmentName
            );
            return StatusCode(
                500,
                ApiResponse<InventoryDto>.Failure("An error occurred while creating inventory item")
            );
        }
    }

    /// <summary>
    /// Update inventory item (Admin or Provider)
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <param name="updateInventoryDto">Inventory update data</param>
    /// <returns>Updated inventory item details</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> UpdateInventory(
        int id,
        [FromBody] UpdateInventoryDto updateInventoryDto
    )
    {
        try
        {
            _logger.LogInformation("Updating inventory item {InventoryId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<InventoryDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _inventoryService.UpdateInventoryAsync(id, updateInventoryDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {InventoryId}", id);
            return StatusCode(
                500,
                ApiResponse<InventoryDto>.Failure("An error occurred while updating inventory item")
            );
        }
    }

    /// <summary>
    /// Delete inventory item (Admin only)
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteInventory(int id)
    {
        try
        {
            _logger.LogInformation("Admin deleting inventory item {InventoryId}", id);

            var result = await _inventoryService.DeleteInventoryAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item {InventoryId}", id);
            return StatusCode(
                500,
                ApiResponse<bool>.Failure("An error occurred while deleting inventory item")
            );
        }
    }
}
