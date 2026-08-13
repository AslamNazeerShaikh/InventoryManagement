namespace InventoryManagement.Domain.DTOs;

/// <summary>
/// Standard envelope for all API responses, providing a uniform success flag, human-readable
/// message, optional payload and a list of error details.
/// </summary>
/// <typeparam name="T">Type of the payload carried on success.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Human-readable summary suitable for display.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Payload returned on success; <c>default</c> on failure.</summary>
    public T? Data { get; set; }

    /// <summary>Zero or more error detail strings on failure.</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Creates a successful response wrapping <paramref name="data"/>.</summary>
    public static ApiResponse<T> Success(T data, string message = "Operation successful")
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data,
        };
    }

    /// <summary>Creates a failed response with a message and optional error details.</summary>
    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors ?? new List<string>(),
        };
    }
}

/// <summary>A single page of results together with pagination metadata.</summary>
/// <typeparam name="T">Item type.</typeparam>
public class PagedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IEnumerable<T> Data { get; set; } = new List<T>();

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>1-based current page number.</summary>
    public int PageNumber { get; set; }

    /// <summary>Maximum items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages (derived).</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether a next page exists (derived).</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Whether a previous page exists (derived).</summary>
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>Authentication result returned to clients after login or refresh.</summary>
public class AuthResponseDto
{
    /// <summary>Signed JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Opaque refresh token (returned to the client once; stored server-side only as a hash).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>UTC expiry of the access token.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Authenticated user's profile projection.</summary>
    public UserDto User { get; set; } = null!;
}

/// <summary>Request body carrying a refresh token to be exchanged for new tokens.</summary>
public class RefreshTokenDto
{
    /// <summary>The refresh token previously issued to the client.</summary>
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = false)]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Headline statistics rendered on the dashboard.</summary>
public class DashboardStatsDto
{
    /// <summary>Total number of inventory items.</summary>
    public int TotalInventories { get; set; }

    /// <summary>Number of available items.</summary>
    public int AvailableInventories { get; set; }

    /// <summary>Number of fully assigned items.</summary>
    public int AssignedInventories { get; set; }

    /// <summary>Number of items approaching expiry.</summary>
    public int ExpiringInventories { get; set; }

    /// <summary>Number of items at or below the low-stock threshold.</summary>
    public int LowStockInventories { get; set; }

    /// <summary>Total number of users.</summary>
    public int TotalUsers { get; set; }

    /// <summary>Number of active assignments.</summary>
    public int ActiveAssignments { get; set; }

    /// <summary>Number of overdue assignments.</summary>
    public int OverdueAssignments { get; set; }
}
