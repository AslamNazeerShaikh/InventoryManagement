using InventoryManagement.API.Controllers;
using InventoryManagement.API.Tests.Fakes;
using InventoryManagement.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.API.Tests;

/// <summary>Unit tests for <see cref="DashboardController"/> using a hand-rolled fake service.</summary>
public class DashboardControllerTests
{
    private static DashboardController CreateController(FakeDashboardService service) =>
        new(service, NullLogger<DashboardController>.Instance);

    [Fact]
    public async Task GetDashboardStats_ReturnsOk_WithServiceData()
    {
        var service = new FakeDashboardService
        {
            StatsToReturn = new DashboardStatsDto { TotalInventories = 42 },
        };
        var controller = CreateController(service);

        var actionResult = await controller.GetDashboardStats(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<DashboardStatsDto>>(ok.Value);
        Assert.True(response.IsSuccess);
        Assert.Equal(42, response.Data!.TotalInventories);
    }

    [Fact]
    public async Task GetRecentInventories_PassesCountToService()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(service);

        await controller.GetRecentInventories(15, CancellationToken.None);

        Assert.Equal(15, service.LastRecentInventoriesCount);
    }

    [Fact]
    public async Task GetRecentInventories_ReturnsOk()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(service);

        var actionResult = await controller.GetRecentInventories(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<IEnumerable<InventoryDto>>>(ok.Value);
        Assert.True(response.IsSuccess);
    }
}
