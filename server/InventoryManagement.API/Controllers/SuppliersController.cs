using InventoryManagement.API.Infrastructure;
using InventoryManagement.API.Infrastructure.Authorization;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>Managed supplier/vendor directory endpoints. Reads for all roles; writes for Admin/Provider;
/// deletes for Admin only. Services return <c>Result&lt;T&gt;</c>; the base controller maps status codes.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[HasPermission(Permissions.Suppliers.Read)]
public class SuppliersController : ApiControllerBase
{
    private readonly ISupplierService _supplierService;

    /// <summary>Creates the controller.</summary>
    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>Lists suppliers, optionally only active ones.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierDto>>>> GetSuppliers(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _supplierService.GetAllAsync(activeOnly, cancellationToken));

    /// <summary>Gets a deterministic page of suppliers.</summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierDto>>>> GetSuppliersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _supplierService.GetPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Gets a supplier by identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplierById(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _supplierService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a supplier (Admin or Provider).</summary>
    [HttpPost]
    [HasPermission(Permissions.Suppliers.Manage)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier(
        [FromBody] CreateSupplierDto createDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _supplierService.CreateAsync(createDto, cancellationToken);
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetSupplierById),
                    new { id = value.Id },
                    ApiResponse<SupplierDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates a supplier (Admin or Provider).</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Suppliers.Manage)]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(
        int id,
        [FromBody] UpdateSupplierDto updateDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _supplierService.UpdateAsync(id, updateDto, cancellationToken));

    /// <summary>Deletes a supplier that has no linked items (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Suppliers.Manage)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSupplier(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _supplierService.DeleteAsync(id, cancellationToken));
}
