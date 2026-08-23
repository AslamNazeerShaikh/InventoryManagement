using System.Security.Cryptography;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Data.Providers;
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
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
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

        WarnIfUniquenessCannotBeEnforced(context, logger);

        // Seed the tenant's permission catalog and system roles (idempotent).
        await SeedTenantRbacAsync(context, cancellationToken).ConfigureAwait(false);

        var adminEmail = configuration["SeedData:AdminEmail"] ?? "admin@inventorymanagement.com";

        var adminUser = await context
            .Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail && !u.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (adminUser is null)
        {
            var configuredPassword = configuration["SeedData:AdminPassword"];
            var adminPassword = string.IsNullOrWhiteSpace(configuredPassword)
                ? GenerateStrongPassword()
                : configuredPassword;

            adminUser = new User
            {
                Name = "System Administrator",
                Email = adminEmail,
                PasswordHash = hasher.Hash(adminPassword),
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
        }

        // Ensure the seeded administrator holds the Administrator role.
        await EnsureAdminRoleAsync(context, adminUser.Id, cancellationToken).ConfigureAwait(false);

        return serviceProvider;
    }

    /// <summary>
    /// Emits a critical log entry when the configured provider has no <see cref="IDatabaseProviderDialect"/>.
    /// <para>
    /// Such a provider silently loses two guarantees: the filtered per-tenant "unique when present"
    /// indexes on <c>Inventory.Barcode</c>/<c>SerialNumber</c> are not created, and store-level
    /// unique violations are no longer recognized (so they surface as HTTP 500 instead of 409).
    /// Because the check-then-act pre-check in the application layer cannot prevent a concurrent
    /// duplicate on its own, that combination silently readmits duplicate rows. Startup is not
    /// aborted — an unlisted provider may still be operationally sound — but the loss of an
    /// integrity constraint must never be inferred only from its absence.
    /// </para>
    /// </summary>
    private static void WarnIfUniquenessCannotBeEnforced(AppDbContext context, ILogger logger)
    {
        var providerName = context.Database.ProviderName;
        if (DatabaseProviderDialects.CanEnforceUniqueWhenPresent(providerName))
        {
            return;
        }

        logger.LogCritical(
            "Database provider {Provider} has no registered dialect: the per-tenant unique-when-present "
                + "indexes on Inventory.Barcode/SerialNumber were NOT created and unique violations will "
                + "surface as HTTP 500 instead of 409, so concurrent creates can persist duplicate "
                + "barcodes/serial numbers. Register an IDatabaseProviderDialect for this provider "
                + "before running it in production.",
            providerName ?? "(none)"
        );
    }

    /// <summary>Seeds the permission catalog and system roles (with default grants) for the current tenant. Idempotent.</summary>
    private static async Task SeedTenantRbacAsync(
        AppDbContext context,
        CancellationToken cancellationToken
    )
    {
        // 1. Permission catalog: insert any codes missing for this tenant.
        var existingCodes = await context
            .Permissions.Select(p => p.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missing = Permissions.All.Where(def => !existingCodes.Contains(def.Code)).ToList();
        foreach (var def in missing)
        {
            await context
                .Permissions.AddAsync(
                    new Permission
                    {
                        Code = def.Code,
                        Description = def.Description,
                        Category = def.Category,
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        if (missing.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var permissionIdByCode = await context
            .Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken)
            .ConfigureAwait(false);

        // 2. System roles and their default permission grants.
        foreach (var roleName in SystemRoles.All)
        {
            var role = await context
                .Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken)
                .ConfigureAwait(false);
            if (role is null)
            {
                role = new Role
                {
                    Name = roleName,
                    IsSystem = true,
                    Description = $"Built-in {roleName} role.",
                };
                await context.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var grantedPermissionIds = await context
                .RolePermissions.Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var code in SystemRoles.DefaultGrants[roleName])
            {
                if (
                    permissionIdByCode.TryGetValue(code, out var permissionId)
                    && !grantedPermissionIds.Contains(permissionId)
                )
                {
                    await context
                        .RolePermissions.AddAsync(
                            new RolePermission { RoleId = role.Id, PermissionId = permissionId },
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ensures the administrator user is a member of the Administrator role. Idempotent.</summary>
    private static async Task EnsureAdminRoleAsync(
        AppDbContext context,
        int adminUserId,
        CancellationToken cancellationToken
    )
    {
        var adminRole = await context
            .Roles.FirstOrDefaultAsync(r => r.Name == SystemRoles.Administrator, cancellationToken)
            .ConfigureAwait(false);
        if (adminRole is null)
        {
            return;
        }

        var alreadyAssigned = await context
            .UserRoles.AnyAsync(
                ur => ur.UserId == adminUserId && ur.RoleId == adminRole.Id,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (!alreadyAssigned)
        {
            await context
                .UserRoles.AddAsync(
                    new UserRole { UserId = adminUserId, RoleId = adminRole.Id },
                    cancellationToken
                )
                .ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Generates a cryptographically strong, URL-safe password.</summary>
    private static string GenerateStrongPassword() =>
        Convert
            .ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', 'A')
            .Replace('/', 'B')
            .Replace('=', 'C');
}
