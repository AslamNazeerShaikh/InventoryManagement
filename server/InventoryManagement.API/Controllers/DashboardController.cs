using InventoryManagement.API.Infrastructure;
using InventoryManagement.API.Infrastructure.Authorization;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>
/// Dashboard endpoints. Aggregate endpoints execute their queries <em>sequentially</em> against the
/// single scoped <see cref="IDashboardService"/> unit of work — never concurrently — because a
/// DbContext is not thread-safe.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[HasPermission(Permissions.Dashboard.Read)]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    /// <summary>Creates the controller.</summary>
    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger
    )
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>Gets dashboard headline statistics.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats(
        CancellationToken cancellationToken
    ) => HandleResult(await _dashboardService.GetDashboardStatsAsync(cancellationToken));

    /// <summary>Gets the most recent inventory items (count clamped 1–50).</summary>
    [HttpGet("recent-inventories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetRecentInventories(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _dashboardService.GetRecentInventoriesAsync(count, cancellationToken));

    /// <summary>Gets the most recent assignments (Admin or Provider; count clamped 1–50).</summary>
    [HttpGet("recent-assignments")]
    [HasPermission(Permissions.Assignments.Read)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetRecentAssignments(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _dashboardService.GetRecentAssignmentsAsync(count, cancellationToken));

    /// <summary>Gets items approaching expiry.</summary>
    [HttpGet("alerts/expiry")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetExpiryAlerts(
        CancellationToken cancellationToken
    ) => HandleResult(await _dashboardService.GetExpiryAlertsAsync(cancellationToken));

    /// <summary>Gets low-stock items.</summary>
    [HttpGet("alerts/low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryDto>>>> GetLowStockAlerts(
        CancellationToken cancellationToken
    ) => HandleResult(await _dashboardService.GetLowStockAlertsAsync(cancellationToken));

    /// <summary>Gets overdue assignments (Admin or Provider).</summary>
    [HttpGet("alerts/overdue")]
    [HasPermission(Permissions.Assignments.Read)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetOverdueAlerts(CancellationToken cancellationToken) =>
        HandleResult(await _dashboardService.GetOverdueAlertsAsync(cancellationToken));

    /// <summary>Gets a combined alerts summary. Overdue alerts are included only for privileged callers.</summary>
    [HttpGet("alerts/summary")]
    public async Task<ActionResult<ApiResponse<object>>> GetAlertsSummary(
        CancellationToken cancellationToken
    )
    {
        var expiry = await _dashboardService.GetExpiryAlertsAsync(cancellationToken);
        var lowStock = await _dashboardService.GetLowStockAlertsAsync(cancellationToken);

        var canViewAssignments = HasPermission(Permissions.Assignments.Read);
        IEnumerable<InventoryAssignmentDto> overdue = new List<InventoryAssignmentDto>();
        if (canViewAssignments)
        {
            var overdueResult = await _dashboardService.GetOverdueAlertsAsync(cancellationToken);
            overdue = overdueResult.Value ?? Enumerable.Empty<InventoryAssignmentDto>();
        }

        var summary = new
        {
            ExpiryAlerts = expiry.Value ?? Enumerable.Empty<InventoryDto>(),
            ExpiryCount = expiry.Value?.Count() ?? 0,
            LowStockAlerts = lowStock.Value ?? Enumerable.Empty<InventoryDto>(),
            LowStockCount = lowStock.Value?.Count() ?? 0,
            OverdueAlerts = overdue,
            OverdueCount = overdue.Count(),
            HasPermissionForOverdue = canViewAssignments,
        };

        return Ok(ApiResponse<object>.Success(summary, "Alerts summary retrieved successfully"));
    }

    /// <summary>Gets a combined dashboard overview. Assignment data is included only for privileged callers.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<object>>> GetDashboardOverview(
        CancellationToken cancellationToken
    )
    {
        var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        var recentInventories = await _dashboardService.GetRecentInventoriesAsync(
            5,
            cancellationToken
        );
        var expiry = await _dashboardService.GetExpiryAlertsAsync(cancellationToken);
        var lowStock = await _dashboardService.GetLowStockAlertsAsync(cancellationToken);

        var canViewAssignments = HasPermission(Permissions.Assignments.Read);
        IEnumerable<InventoryAssignmentDto> recentAssignments = new List<InventoryAssignmentDto>();
        IEnumerable<InventoryAssignmentDto> overdue = new List<InventoryAssignmentDto>();
        if (canViewAssignments)
        {
            var recentAssignmentsResult = await _dashboardService.GetRecentAssignmentsAsync(
                5,
                cancellationToken
            );
            recentAssignments =
                recentAssignmentsResult.Value ?? Enumerable.Empty<InventoryAssignmentDto>();

            var overdueResult = await _dashboardService.GetOverdueAlertsAsync(cancellationToken);
            overdue = overdueResult.Value ?? Enumerable.Empty<InventoryAssignmentDto>();
        }

        var overview = new
        {
            Stats = stats.Value,
            RecentInventories = recentInventories.Value ?? Enumerable.Empty<InventoryDto>(),
            RecentAssignments = recentAssignments,
            Alerts = new
            {
                Expiry = expiry.Value ?? Enumerable.Empty<InventoryDto>(),
                LowStock = lowStock.Value ?? Enumerable.Empty<InventoryDto>(),
                Overdue = overdue,
            },
            AlertCounts = new
            {
                ExpiryCount = expiry.Value?.Count() ?? 0,
                LowStockCount = lowStock.Value?.Count() ?? 0,
                OverdueCount = overdue.Count(),
            },
            UserPermissions = new
            {
                CanViewAssignments = canViewAssignments,
                CanManageUsers = HasPermission(Permissions.Users.Manage),
                CanManageInventory = HasPermission(Permissions.Inventory.Manage),
            },
        };

        return Ok(
            ApiResponse<object>.Success(overview, "Dashboard overview retrieved successfully")
        );
    }
}
