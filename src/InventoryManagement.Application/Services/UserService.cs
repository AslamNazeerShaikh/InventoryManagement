using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
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
    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var users = await _unitOfWork
            .Users.ListAsync(orderBy: q => q.OrderBy(u => u.Name), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<UserDto>>.Success(users.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null
            ? ApiResponse<UserDto>.Failure("User not found")
            : ApiResponse<UserDto>.Success(user.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken)
            .ConfigureAwait(false);
        return user is null
            ? ApiResponse<UserDto>.Failure("User not found")
            : ApiResponse<UserDto>.Success(user.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UserDto>> CreateUserAsync(
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
            return ApiResponse<UserDto>.Failure("Email already exists");
        }

        var user = createUserDto.ToEntity();
        user.PasswordHash = _passwordHasher.Hash(createUserDto.Password);

        await _unitOfWork.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created user {UserId} ({Email}).", user.Id, user.Email);
        return ApiResponse<UserDto>.Success(user.ToDto(), "User created successfully");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UserDto>> UpdateUserAsync(
        int id,
        UpdateUserDto updateUserDto,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApiResponse<UserDto>.Failure("User not found");
        }

        if (
            user.Email != updateUserDto.Email
            && await _unitOfWork
                .Users.IsEmailExistsAsync(updateUserDto.Email, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return ApiResponse<UserDto>.Failure("Email already exists");
        }

        updateUserDto.UpdateEntity(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated user {UserId}.", id);
        return ApiResponse<UserDto>.Success(user.ToDto(), "User updated successfully");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> DeleteUserAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApiResponse<bool>.Failure("User not found");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Soft-deleted user {UserId}.", id);
        return ApiResponse<bool>.Success(true, "User deleted successfully");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<UserDto>>> GetNursePractitionersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var nurses = await _unitOfWork.Users.GetNursePractitionersAsync(cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<UserDto>>.Success(nurses.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<UserDto>>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        var users = await _unitOfWork.Users.GetActiveUsersAsync(cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<UserDto>>.Success(users.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersPagedAsync(
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

        return ApiResponse<PagedResult<UserDto>>.Success(result);
    }
}
