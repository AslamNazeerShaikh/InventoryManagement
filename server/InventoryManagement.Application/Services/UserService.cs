using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>User management service. Persists credentials only as Microsoft PBKDF2 hashes and
/// enforces email uniqueness. Unexpected faults propagate to the global exception handler.</summary>
public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    /// <summary>Creates the user service.</summary>
    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var users = await _unitOfWork
            .Users.ListAsync(
                include: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role),
                orderBy: q => q.OrderBy(u => u.Name),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return Result<IEnumerable<UserDto>>.Success(users.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> GetUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var user = await LoadUserWithRolesAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result<UserDto>.NotFound("User not found")
            : Result<UserDto>.Success(user.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork
            .Users.GetByEmailAsync(email, cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? Result<UserDto>.NotFound("User not found")
            : Result<UserDto>.Success(user.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserDto createUserDto,
        CancellationToken cancellationToken = default
    )
    {
        if (
            await _unitOfWork
                .Users.IsEmailExistsAsync(createUserDto.Email, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<UserDto>.Conflict("Email already exists");
        }

        var user = createUserDto.ToEntity();
        user.PasswordHash = _passwordHasher.Hash(createUserDto.Password);

        await _unitOfWork.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await AssignRolesAsync(user.Id, createUserDto.RoleIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created user {UserId} ({Email}).", user.Id, user.Email);
        var created = await LoadUserWithRolesAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return Result<UserDto>.Success(created!.ToDto(), "User created successfully");
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> UpdateUserAsync(
        int id,
        UpdateUserDto updateUserDto,
        bool allowPrivilegedFields,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork
            .Users.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            return Result<UserDto>.NotFound("User not found");
        }

        if (
            user.Email != updateUserDto.Email
            && await _unitOfWork
                .Users.IsEmailExistsAsync(updateUserDto.Email, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return Result<UserDto>.Conflict("Email already exists");
        }

        user.Name = updateUserDto.Name;
        user.Email = updateUserDto.Email;
        if (allowPrivilegedFields)
        {
            user.IsActive = updateUserDto.IsActive;
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Role membership is a privileged change; a null role set (or a self-service edit) leaves it untouched.
        if (allowPrivilegedFields && updateUserDto.RoleIds is not null)
        {
            await AssignRolesAsync(id, updateUserDto.RoleIds, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Updated user {UserId}.", id);
        var updated = await LoadUserWithRolesAsync(id, cancellationToken).ConfigureAwait(false);
        return Result<UserDto>.Success(updated!.ToDto(), "User updated successfully");
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteUserAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork
            .Users.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            return Result<bool>.NotFound("User not found");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted user {UserId}.", id);
        return Result<bool>.Success(true, "User deleted successfully");
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserDto>>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var users = await _unitOfWork
            .Users.GetActiveUsersAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result<IEnumerable<UserDto>>.Success(users.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        var page = await _unitOfWork
            .Users.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderByDescending(u => u.CreatedAt),
                include: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<UserDto>
        {
            Data = page.Items.ToDto(),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return Result<PagedResult<UserDto>>.Success(result);
    }

    /// <summary>Loads a user (tracking-free) with their role memberships for projection.</summary>
    private Task<User?> LoadUserWithRolesAsync(int id, CancellationToken cancellationToken) =>
        _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == id,
            include: q => q.Include(u => u.UserRoles).ThenInclude(ur => ur.Role),
            cancellationToken: cancellationToken
        );

    /// <summary>Reconciles a user's role memberships to exactly the supplied (existing) role ids.</summary>
    private async Task AssignRolesAsync(
        int userId,
        IReadOnlyCollection<int> roleIds,
        CancellationToken cancellationToken
    )
    {
        // Keep only ids that resolve to a role in the current tenant.
        var validRoleIds = new HashSet<int>();
        foreach (var roleId in roleIds.Distinct())
        {
            if (
                await _unitOfWork.Roles.GetByIdAsync(roleId, cancellationToken).ConfigureAwait(false)
                is not null
            )
            {
                validRoleIds.Add(roleId);
            }
        }

        var existing = await _unitOfWork
            .UserRoles.GetForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var existingRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();

        foreach (var membership in existing.Where(ur => !validRoleIds.Contains(ur.RoleId)))
        {
            _unitOfWork.UserRoles.Remove(membership);
        }

        foreach (var roleId in validRoleIds.Where(rid => !existingRoleIds.Contains(rid)))
        {
            await _unitOfWork
                .UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = roleId }, cancellationToken)
                .ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
