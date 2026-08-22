using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>User management operations (CRUD and role-scoped queries).</summary>
public interface IUserService
{
    /// <summary>Lists all users.</summary>
    Task<Result<IEnumerable<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets a user by identifier.</summary>
    Task<Result<UserDto>> GetUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets a user by email.</summary>
    Task<Result<UserDto>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a user with a hashed password.</summary>
    Task<Result<UserDto>> CreateUserAsync(
        CreateUserDto createUserDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates a user's profile. When <paramref name="allowPrivilegedFields"/> is <c>false</c> (a
    /// self-service profile edit) the user's role membership and active status are left unchanged.
    /// </summary>
    Task<Result<UserDto>> UpdateUserAsync(
        int id,
        UpdateUserDto updateUserDto,
        bool allowPrivilegedFields,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes a user.</summary>
    Task<Result<bool>> DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists active users.</summary>
    Task<Result<IEnumerable<UserDto>>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of users.</summary>
    Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
