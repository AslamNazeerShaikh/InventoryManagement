using InventoryManagement.API.Infrastructure;
using InventoryManagement.API.Infrastructure.Authorization;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>
/// Dynamic role and permission administration. Tenants compose the permission catalog into custom
/// roles and assign them to users (via the Users endpoints). All operations require the
/// <c>roles.manage</c> permission.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[HasPermission(Permissions.Roles.Manage)]
public class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    /// <summary>Creates the controller.</summary>
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>Lists the tenant's roles with their granted permission codes.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles(
        CancellationToken cancellationToken
    ) => HandleResult(await _roleService.GetRolesAsync(cancellationToken));

    /// <summary>Returns the assignable permission catalog for the tenant.</summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetPermissionCatalog(
        CancellationToken cancellationToken
    ) => HandleResult(await _roleService.GetPermissionCatalogAsync(cancellationToken));

    /// <summary>Gets a role by identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleById(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _roleService.GetRoleByIdAsync(id, cancellationToken));

    /// <summary>Creates a custom role.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleDto createRoleDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _roleService.CreateRoleAsync(createRoleDto, cancellationToken);
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetRoleById),
                    new { id = value.Id },
                    ApiResponse<RoleDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates a role's name, description and permission grants.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(
        int id,
        [FromBody] UpdateRoleDto updateRoleDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _roleService.UpdateRoleAsync(id, updateRoleDto, cancellationToken));

    /// <summary>Deletes a custom role that has no members.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _roleService.DeleteRoleAsync(id, cancellationToken));
}
