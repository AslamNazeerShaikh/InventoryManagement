using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryAssignment> InventoryAssignments { get; set; }
    public DbSet<IdempotentRequest> IdempotentRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update auditing fields before saving
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Update auditing fields before saving
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e =>
                e.Entity is Domain.Common.BaseEntity
                && (e.State == EntityState.Added || e.State == EntityState.Modified)
            );

        foreach (var entityEntry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
