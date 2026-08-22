using System.Security.Cryptography;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Idempotency;
using InventoryManagement.Infrastructure.Multitenancy;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Extensions;

/// <summary>
/// Composition-root registrations for the Infrastructure layer: persistence, repositories, unit of
/// work, security primitives, idempotency and configuration options binding/validation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers all Infrastructure services and binds/validates related configuration.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        // Ambient multi-tenancy (scoped): resolved per request by the presentation layer and consumed
        // by AppDbContext to filter every query to the caller's tenant and stamp it on insert.
        services.AddScoped<ITenantContext, TenantContext>();

        // System services.
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Security: Microsoft-based password hashing, JWT token service and signing-key resolution.
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.TryAddSingleton<ISecretClient, EnvironmentFileSecretClient>();
        services.AddSingleton<IJwtSigningKeyProvider, JwtSigningKeyProvider>();
        services.AddSingleton<ITokenService, TokenService>();

        // Data access.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryAssignmentRepository, InventoryAssignmentRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IMaintenanceScheduleRepository, MaintenanceScheduleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Idempotency store and background purge.
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddHostedService<IdempotencyCleanupService>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds an initial administrator if none exists. The admin
    /// password is sourced from configuration (<c>SeedData:AdminPassword</c>); when absent a strong
    /// random password is generated and logged once so no well-known default credential ships.
    /// </summary>
    public static async Task<IServiceProvider> SeedDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("InventoryManagement.Infrastructure.Seeding");

        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var adminEmail = configuration["SeedData:AdminEmail"] ?? "admin@inventorymanagement.com";

        if (
            await context
                .Users.IgnoreQueryFilters()
                .AnyAsync(u => u.Email == adminEmail && !u.IsDeleted, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return serviceProvider;
        }

        var configuredPassword = configuration["SeedData:AdminPassword"];
        var adminPassword = string.IsNullOrWhiteSpace(configuredPassword)
            ? GenerateStrongPassword()
            : configuredPassword;

        var adminUser = new User
        {
            Name = "System Administrator",
            Email = adminEmail,
            PasswordHash = hasher.Hash(adminPassword),
            IsAdmin = true,
            IsProvider = false,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedBy = "System",
        };

        await context.Users.AddAsync(adminUser, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            logger.LogWarning(
                "Seeded administrator {Email} with a generated password: {Password}. "
                    + "Change it immediately and configure SeedData:AdminPassword for reproducible setups.",
                adminEmail,
                adminPassword
            );
        }
        else
        {
            logger.LogInformation("Seeded administrator {Email} from configuration.", adminEmail);
        }

        return serviceProvider;
    }

    /// <summary>Generates a cryptographically strong, URL-safe password.</summary>
    private static string GenerateStrongPassword() =>
        Convert
            .ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', 'A')
            .Replace('/', 'B')
            .Replace('=', 'C');
}
