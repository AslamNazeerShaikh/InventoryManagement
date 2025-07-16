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
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger
    )
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics overview
    /// </summary>
    /// <returns>Dashboard statistics including totals and counts</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        try
        {
            _logger.LogInformation("Requesting dashboard statistics");
            var result = await _dashboardService.GetDashboardStatsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(
                500,
                ApiResponse<DashboardStatsDto>.Failure(
                    "An error occurred while retrieving dashboard statistics"
                )
            );
        }
    }

    /// <summary>
    /// Get recent inventory items
    /// </summary>
    /// <param name="count">Number of recent items to retrieve (default: 10, max: 50)</param>
    /// <returns>List of recent inventory items</returns>
    [HttpGet("recent-inventories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetRecentInventories(
        [FromQuery] int count = 10
    )
    {
        try
        {
            if (count > 50)
                count = 50; // Limit to prevent excessive data
            if (count < 1)
                count = 1;

            _logger.LogInformation("Requesting {Count} recent inventory items", count);
            var result = await _dashboardService.GetRecentInventoriesAsync(count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent inventory items");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving recent inventory items"
                )
            );
        }
    }

    /// <summary>
    /// Get recent assignments
    /// </summary>
    /// <param name="count">Number of recent assignments to retrieve (default: 10, max: 50)</param>
    /// <returns>List of recent assignments</returns>
    [HttpGet("recent-assignments")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetRecentAssignments([FromQuery] int count = 10)
    {
        try
        {
            if (count > 50)
                count = 50; // Limit to prevent excessive data
            if (count < 1)
                count = 1;

            _logger.LogInformation("Requesting {Count} recent assignments", count);
            var result = await _dashboardService.GetRecentAssignmentsAsync(count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent assignments");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving recent assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get expiry alerts (items expiring within 3-6 months)
    /// </summary>
    /// <returns>List of inventory items requiring expiry attention</returns>
    [HttpGet("alerts/expiry")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetExpiryAlerts()
    {
        try
        {
            _logger.LogInformation("Requesting expiry alerts");
            var result = await _dashboardService.GetExpiryAlertsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiry alerts");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving expiry alerts"
                )
            );
        }
    }

    /// <summary>
    /// Get low stock alerts
    /// </summary>
    /// <returns>List of inventory items with low stock levels</returns>
    [HttpGet("alerts/low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetLowStockAlerts()
    {
        try
        {
            _logger.LogInformation("Requesting low stock alerts");
            var result = await _dashboardService.GetLowStockAlertsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock alerts");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryDto>>.Failure(
                    "An error occurred while retrieving low stock alerts"
                )
            );
        }
    }

    /// <summary>
    /// Get overdue assignment alerts (Admin or Provider)
    /// </summary>
    /// <returns>List of overdue assignments requiring attention</returns>
    [HttpGet("alerts/overdue")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetOverdueAlerts()
    {
        try
        {
            _logger.LogInformation("Requesting overdue assignment alerts");
            var result = await _dashboardService.GetOverdueAlertsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue alerts");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving overdue alerts"
                )
            );
        }
    }

    /// <summary>
    /// Get all alerts in one request for dashboard overview
    /// </summary>
    /// <returns>Combined alerts data</returns>
    [HttpGet("alerts/summary")]
    public async Task<ActionResult<object>> GetAlertsSummary()
    {
        try
        {
            _logger.LogInformation("Requesting alerts summary");

            // Get all alerts concurrently for better performance
            var expiryTask = _dashboardService.GetExpiryAlertsAsync();
            var lowStockTask = _dashboardService.GetLowStockAlertsAsync();

            // Only get overdue alerts if user has permission
            var isAdminOrProvider =
                User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True"
                || User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";

            Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>>? overdueTask = null;
            if (isAdminOrProvider)
            {
                overdueTask = _dashboardService.GetOverdueAlertsAsync();
            }

            // Wait for all tasks to complete
            await Task.WhenAll(expiryTask, lowStockTask);
            if (overdueTask != null)
            {
                await overdueTask;
            }

            var alertsSummary = new
            {
                ExpiryAlerts = expiryTask.Result.IsSuccess
                    ? expiryTask.Result.Data
                    : new List<InventoryDto>(),
                ExpiryCount = expiryTask.Result.IsSuccess
                    ? expiryTask.Result.Data?.Count() ?? 0
                    : 0,
                LowStockAlerts = lowStockTask.Result.IsSuccess
                    ? lowStockTask.Result.Data
                    : new List<InventoryDto>(),
                LowStockCount = lowStockTask.Result.IsSuccess
                    ? lowStockTask.Result.Data?.Count() ?? 0
                    : 0,
                OverdueAlerts = overdueTask?.Result.IsSuccess == true
                    ? overdueTask.Result.Data
                    : new List<InventoryAssignmentDto>(),
                OverdueCount = overdueTask?.Result.IsSuccess == true
                    ? overdueTask.Result.Data?.Count() ?? 0
                    : 0,
                HasPermissionForOverdue = isAdminOrProvider,
            };

            return Ok(
                ApiResponse<object>.Success(alertsSummary, "Alerts summary retrieved successfully")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts summary");
            return StatusCode(
                500,
                ApiResponse<object>.Failure("An error occurred while retrieving alerts summary")
            );
        }
    }

    /// <summary>
    /// Get combined dashboard data in one request for efficiency
    /// </summary>
    /// <returns>Complete dashboard data including stats, recent items, and alerts</returns>
    [HttpGet("overview")]
    public async Task<ActionResult<object>> GetDashboardOverview()
    {
        try
        {
            _logger.LogInformation("Requesting dashboard overview");

            // Execute multiple requests concurrently for better performance
            var statsTask = _dashboardService.GetDashboardStatsAsync();
            var recentInventoriesTask = _dashboardService.GetRecentInventoriesAsync(5);
            var expiryAlertsTask = _dashboardService.GetExpiryAlertsAsync();
            var lowStockAlertsTask = _dashboardService.GetLowStockAlertsAsync();

            // Only get assignments and overdue alerts if user has permission
            var isAdminOrProvider =
                User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True"
                || User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";

            Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>>? recentAssignmentsTask = null;
            Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>>? overdueAlertsTask = null;

            if (isAdminOrProvider)
            {
                recentAssignmentsTask = _dashboardService.GetRecentAssignmentsAsync(5);
                overdueAlertsTask = _dashboardService.GetOverdueAlertsAsync();
            }

            // Wait for all tasks to complete
            await Task.WhenAll(
                statsTask,
                recentInventoriesTask,
                expiryAlertsTask,
                lowStockAlertsTask
            );
            if (recentAssignmentsTask != null && overdueAlertsTask != null)
            {
                await Task.WhenAll(recentAssignmentsTask, overdueAlertsTask);
            }

            var dashboardData = new
            {
                Stats = statsTask.Result.IsSuccess ? statsTask.Result.Data : null,
                RecentInventories = recentInventoriesTask.Result.IsSuccess
                    ? recentInventoriesTask.Result.Data
                    : new List<InventoryDto>(),
                RecentAssignments = recentAssignmentsTask?.Result.IsSuccess == true
                    ? recentAssignmentsTask.Result.Data
                    : new List<InventoryAssignmentDto>(),
                Alerts = new
                {
                    Expiry = expiryAlertsTask.Result.IsSuccess
                        ? expiryAlertsTask.Result.Data
                        : new List<InventoryDto>(),
                    LowStock = lowStockAlertsTask.Result.IsSuccess
                        ? lowStockAlertsTask.Result.Data
                        : new List<InventoryDto>(),
                    Overdue = overdueAlertsTask?.Result.IsSuccess == true
                        ? overdueAlertsTask.Result.Data
                        : new List<InventoryAssignmentDto>(),
                },
                AlertCounts = new
                {
                    ExpiryCount = expiryAlertsTask.Result.IsSuccess
                        ? expiryAlertsTask.Result.Data?.Count() ?? 0
                        : 0,
                    LowStockCount = lowStockAlertsTask.Result.IsSuccess
                        ? lowStockAlertsTask.Result.Data?.Count() ?? 0
                        : 0,
                    OverdueCount = overdueAlertsTask?.Result.IsSuccess == true
                        ? overdueAlertsTask.Result.Data?.Count() ?? 0
                        : 0,
                },
                UserPermissions = new
                {
                    CanViewAssignments = isAdminOrProvider,
                    CanManageUsers = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True",
                    IsAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True",
                    IsProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True",
                },
            };

            return Ok(
                ApiResponse<object>.Success(
                    dashboardData,
                    "Dashboard overview retrieved successfully"
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return StatusCode(
                500,
                ApiResponse<object>.Failure("An error occurred while retrieving dashboard overview")
            );
        }
    }
}
