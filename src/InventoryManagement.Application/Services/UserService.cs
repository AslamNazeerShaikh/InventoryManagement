using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var userDtos = users.ToDto();
            return ApiResponse<IEnumerable<UserDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<UserDto>>.Failure(
                $"Error retrieving users: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.Failure("User not found");
            }

            return ApiResponse<UserDto>.Success(user.ToDto());
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.Failure($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                return ApiResponse<UserDto>.Failure("User not found");
            }

            return ApiResponse<UserDto>.Success(user.ToDto());
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.Failure($"Error retrieving user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            // Validate email uniqueness
            if (await _unitOfWork.Users.IsEmailExistsAsync(createUserDto.Email))
            {
                return ApiResponse<UserDto>.Failure("Email already exists");
            }

            // Create user entity
            var user = createUserDto.ToEntity();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

            // Add user
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveAsync();

            return ApiResponse<UserDto>.Success(user.ToDto(), "User created successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.Failure($"Error creating user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.Failure("User not found");
            }

            // Check if email is being changed and is unique
            if (
                user.Email != updateUserDto.Email
                && await _unitOfWork.Users.IsEmailExistsAsync(updateUserDto.Email)
            )
            {
                return ApiResponse<UserDto>.Failure("Email already exists");
            }

            // Update user
            updateUserDto.UpdateEntity(user);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveAsync();

            return ApiResponse<UserDto>.Success(user.ToDto(), "User updated successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.Failure($"Error updating user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<bool>.Failure("User not found");
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveAsync();

            return ApiResponse<bool>.Success(true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Failure($"Error deleting user: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetNursePractitionersAsync()
    {
        try
        {
            var nurses = await _unitOfWork.Users.GetNursePractitionersAsync();
            var nurseDtos = nurses.ToDto();
            return ApiResponse<IEnumerable<UserDto>>.Success(nurseDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<UserDto>>.Failure(
                $"Error retrieving nurse practitioners: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetActiveUsersAsync()
    {
        try
        {
            var users = await _unitOfWork.Users.GetActiveUsersAsync();
            var userDtos = users.ToDto();
            return ApiResponse<IEnumerable<UserDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<UserDto>>.Failure(
                $"Error retrieving active users: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        try
        {
            pageSize = Math.Min(pageSize, BusinessConstants.Pagination.MaxPageSize);

            var users = await _unitOfWork.Users.GetPagedAsync(pageNumber, pageSize);
            var totalCount = await _unitOfWork.Users.CountAsync();

            var pagedResult = new PagedResult<UserDto>
            {
                Data = users.ToDto(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            return ApiResponse<PagedResult<UserDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<UserDto>>.Failure(
                $"Error retrieving paged users: {ex.Message}"
            );
        }
    }
}
