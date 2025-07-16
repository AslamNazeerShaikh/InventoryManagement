using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Database Context
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
        );

        // Repository Registration
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryAssignmentRepository, InventoryAssignmentRepository>();

        // Unit of Work Registration
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task<IServiceProvider> SeedDatabaseAsync(
        this IServiceProvider serviceProvider
    )
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }

        // Seed initial data
        await SeedInitialDataAsync(context);

        return serviceProvider;
    }

    private static async Task SeedInitialDataAsync(AppDbContext context)
    {
        // Check if admin user already exists
        if (!await context.Users.AnyAsync(u => u.Email == "admin@inventorymanagement.com"))
        {
            var adminUser = new Domain.Entities.User
            {
                Name = "System Administrator",
                Email = "admin@inventorymanagement.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // You should use a proper password hashing
                IsAdmin = true,
                IsProvider = false,
                Role = Domain.Enums.UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
