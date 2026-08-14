using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the inventory system. Applies entity configurations,
/// enforces optimistic concurrency via a rotating <see cref="BaseEntity.ConcurrencyToken"/>, and
/// centralizes auditing timestamps on save.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IDateTimeProvider? _clock;

    /// <summary>Creates the context with the supplied options (design-time / testing).</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    /// <summary>Creates the context with an injected clock for deterministic audit timestamps.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider clock)
        : base(options)
    {
        _clock = clock;
    }

    /// <summary>Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Inventory items table.</summary>
    public DbSet<Inventory> Inventories => Set<Inventory>();

    /// <summary>Inventory assignments table.</summary>
    public DbSet<InventoryAssignment> InventoryAssignments => Set<InventoryAssignment>();

    /// <summary>Idempotency records table.</summary>
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Provider-agnostic optimistic concurrency: mark every BaseEntity.ConcurrencyToken as a
        // concurrency token so EF includes it in UPDATE/DELETE predicates.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.ConcurrencyToken))
                    .IsConcurrencyToken();
            }
        }
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndConcurrency();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        ApplyAuditAndConcurrency();
        return base.SaveChanges();
    }

    /// <summary>
    /// Stamps audit timestamps and rotates the concurrency token for added/modified entities so a
    /// concurrent writer that read the previous token fails its conditional update.
    /// </summary>
    private void ApplyAuditAndConcurrency()
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.ConcurrencyToken = Guid.NewGuid();
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.ConcurrencyToken = Guid.NewGuid();
                    break;
            }
        }
    }
}
