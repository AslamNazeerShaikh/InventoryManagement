using InventoryManagement.API.Controllers;
using InventoryManagement.API.Tests.Fakes;
using InventoryManagement.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.API.Tests;

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

        var actionResult = await controller.GetDashboardStats();

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<DashboardStatsDto>>(ok.Value);
        Assert.True(response.IsSuccess);
        Assert.Equal(42, response.Data!.TotalInventories);
    }

    [Fact]
    public async Task GetRecentInventories_ClampsCountToFifty()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(service);

        await controller.GetRecentInventories(count: 1000);

        Assert.Equal(50, service.LastRecentInventoriesCount);
    }

    [Fact]
    public async Task GetRecentInventories_ClampsCountToOne_WhenBelowMinimum()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(service);

        await controller.GetRecentInventories(count: 0);

        Assert.Equal(1, service.LastRecentInventoriesCount);
    }

    [Fact]
    public async Task GetRecentInventories_PassesThroughValidCount()
    {
        var service = new FakeDashboardService();
        var controller = CreateController(service);

        await controller.GetRecentInventories(count: 15);

        Assert.Equal(15, service.LastRecentInventoriesCount);
    }
}
