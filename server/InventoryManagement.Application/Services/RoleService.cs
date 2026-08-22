using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Dynamic role/permission administration for the current tenant. Custom roles compose the tenant's
/// permission catalog; seeded system roles are protected from rename/deletion. All queries are
/// tenant-scoped by the persistence layer's global filter.
/// </summary>
public sealed class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleService> _logger;

    /// <summary>Creates the role service.</summary>
    public RoleService(IUnitOfWork unitOfWork, ILogger<RoleService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var roles = await _unitOfWork
            .Roles.ListWithPermissionsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<RoleDto>>.Success(roles.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> GetRoleByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _unitOfWork
            .Roles.GetWithPermissionsAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return role is null
            ? Result<RoleDto>.NotFound("Role not found")
            : Result<RoleDto>.Success(role.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<PermissionDto>>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await _unitOfWork
            .Permissions.ListCatalogAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<PermissionDto>>.Success(permissions.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> CreateRoleAsync(
        CreateRoleDto createRoleDto,
        CancellationToken cancellationToken = default
    )
    {
        if (
            await _unitOfWork.Roles.GetByNameAsync(createRoleDto.Name, cancellationToken).ConfigureAwait(false)
            is not null
        )
        {
            return Result<RoleDto>.Conflict("A role with that name already exists");
        }

        var role = new Role
        {
            Name = createRoleDto.Name,
            Description = createRoleDto.Description,
            IsSystem = false,
        };
        await _unitOfWork.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SetRolePermissionsAsync(role.Id, createRoleDto.Permissions, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Created role {RoleId} ({RoleName}).", role.Id, role.Name);
        var created = await _unitOfWork
            .Roles.GetWithPermissionsAsync(role.Id, cancellationToken)
            .ConfigureAwait(false);
        return Result<RoleDto>.Success(created!.ToDto(), "Role created successfully");
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> UpdateRoleAsync(
        int id,
        UpdateRoleDto updateRoleDto,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _unitOfWork
            .Roles.GetWithPermissionsAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (role is null)
        {
            return Result<RoleDto>.NotFound("Role not found");
        }

        if (!string.Equals(role.Name, updateRoleDto.Name, StringComparison.Ordinal))
        {
            if (role.IsSystem)
            {
                return Result<RoleDto>.Validation("System roles cannot be renamed");
            }

            var duplicate = await _unitOfWork
                .Roles.GetByNameAsync(updateRoleDto.Name, cancellationToken)
                .ConfigureAwait(false);
            if (duplicate is not null && duplicate.Id != id)
            {
                return Result<RoleDto>.Conflict("A role with that name already exists");
            }

            role.Name = updateRoleDto.Name;
        }

        if (!role.IsSystem)
        {
            role.Description = updateRoleDto.Description;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Permission grants are editable for both custom and system roles.
        await SetRolePermissionsAsync(id, updateRoleDto.Permissions, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Updated role {RoleId}.", id);
        var updated = await _unitOfWork
            .Roles.GetWithPermissionsAsync(id, cancellationToken)
            .ConfigureAwait(false);
        return Result<RoleDto>.Success(updated!.ToDto(), "Role updated successfully");
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteRoleAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return Result<bool>.NotFound("Role not found");
        }
        if (role.IsSystem)
        {
            return Result<bool>.Conflict("System roles cannot be deleted");
        }

        var hasMembers = await _unitOfWork
            .UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken)
            .ConfigureAwait(false);
        if (hasMembers)
        {
            return Result<bool>.Conflict("Cannot delete a role that is still assigned to users");
        }

        // Hard delete of configuration; the FK cascade removes the role's permission grants.
        _unitOfWork.Roles.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleted role {RoleId}.", id);
        return Result<bool>.Success(true, "Role deleted successfully");
    }

    /// <summary>Reconciles a role's permission grants to exactly the supplied catalog codes.</summary>
    private async Task SetRolePermissionsAsync(
        int roleId,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken
    )
    {
        var role = await _unitOfWork
            .Roles.GetWithPermissionsAsync(roleId, cancellationToken)
            .ConfigureAwait(false);
        if (role is null)
        {
            return;
        }

        var permissions = await _unitOfWork
            .Permissions.GetByCodesAsync(permissionCodes, cancellationToken)
            .ConfigureAwait(false);
        var wantedPermissionIds = permissions.Select(p => p.Id).ToHashSet();
        var currentPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

        foreach (
            var grant in role
                .RolePermissions.Where(rp => !wantedPermissionIds.Contains(rp.PermissionId))
                .ToList()
        )
        {
            role.RolePermissions.Remove(grant);
        }

        foreach (var permissionId in wantedPermissionIds.Where(pid => !currentPermissionIds.Contains(pid)))
        {
            role.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
