using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryAssignmentService, InventoryAssignmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();

        return services;
    }
}
