using InventoryManagement.API.Infrastructure;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>Inventory catalogue endpoints. Services return <c>Result&lt;T&gt;</c>; the base controller
/// maps it to the correct HTTP status while keeping the <c>ApiResponse&lt;T&gt;</c> body.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class InventoryController : ApiControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    /// <summary>Creates the controller.</summary>
    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger
    )
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>Gets all inventory items.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetAllInventories(
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.GetAllInventoriesAsync(cancellationToken));

    /// <summary>Gets a deterministic page of inventory items.</summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryDto>>>> GetInventoriesPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) =>
        HandleResult(
            await _inventoryService.GetInventoriesPagedAsync(pageNumber, pageSize, cancellationToken)
        );

    /// <summary>Gets an inventory item by identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetInventoryById(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.GetInventoryByIdAsync(id, cancellationToken));

    /// <summary>Gets an inventory item by barcode.</summary>
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetInventoryByBarcode(
        string barcode,
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.GetInventoryByBarcodeAsync(barcode, cancellationToken));

    /// <summary>Searches inventory items using the supplied criteria.</summary>
    [HttpPost("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> SearchInventories(
        [FromBody] InventorySearchDto searchDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.SearchInventoriesAsync(searchDto, cancellationToken));

    /// <summary>Gets available inventory items with remaining stock.</summary>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetAvailableInventories(
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.GetAvailableInventoriesAsync(cancellationToken));

    /// <summary>Gets inventory items expiring within the given number of months (clamped 3–6).</summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetExpiringInventories(
        [FromQuery] int monthsBefore = 3,
        CancellationToken cancellationToken = default
    )
    {
        monthsBefore = Math.Clamp(
            monthsBefore,
            BusinessConstants.ExpiryAlert.MinMonthsBefore,
            BusinessConstants.ExpiryAlert.MaxMonthsBefore
        );
        return HandleResult(
            await _inventoryService.GetExpiringInventoriesAsync(monthsBefore, cancellationToken)
        );
    }

    /// <summary>Gets inventory items at or below the given stock threshold.</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetLowStockInventories(
        [FromQuery] int threshold = BusinessConstants.Inventory.LowStockThreshold,
        CancellationToken cancellationToken = default
    ) =>
        HandleResult(
            await _inventoryService.GetLowStockInventoriesAsync(threshold, cancellationToken)
        );

    /// <summary>Gets inventory items in a category.</summary>
    [HttpGet("category/{category}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryDto>>>
    > GetInventoriesByCategory(string category, CancellationToken cancellationToken) =>
        HandleResult(
            await _inventoryService.GetInventoriesByCategoryAsync(category, cancellationToken)
        );

    /// <summary>Creates a new inventory item (Admin or Provider).</summary>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> CreateInventory(
        [FromBody] CreateInventoryDto createInventoryDto,
        CancellationToken cancellationToken
    )
    {
        if (!int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out var userId))
        {
            return BadRequest(ApiResponse<InventoryDto>.Failure("Invalid user ID"));
        }

        var result = await _inventoryService.CreateInventoryAsync(
            createInventoryDto,
            userId,
            cancellationToken
        );
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetInventoryById),
                    new { id = value.Id },
                    ApiResponse<InventoryDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates an inventory item (Admin or Provider).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> UpdateInventory(
        int id,
        [FromBody] UpdateInventoryDto updateInventoryDto,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await _inventoryService.UpdateInventoryAsync(id, updateInventoryDto, cancellationToken)
        );

    /// <summary>Deletes an inventory item (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteInventory(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _inventoryService.DeleteInventoryAsync(id, cancellationToken));
}
