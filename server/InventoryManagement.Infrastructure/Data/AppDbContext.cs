using System.Linq.Expressions;
using System.Reflection;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Security;
using InventoryManagement.Infrastructure.Data.Providers;
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
    private readonly ITenantContext? _tenant;

    /// <summary>Creates the context with the supplied options (design-time / testing).</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    /// <summary>
    /// Creates the context with an injected clock and tenant context (runtime DI). The container
    /// selects this greediest satisfiable constructor, so production always receives both; the
    /// options-only constructor above remains for design-time tooling and tests.
    /// </summary>
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDateTimeProvider clock,
        ITenantContext tenant
    )
        : base(options)
    {
        _clock = clock;
        _tenant = tenant;
    }

    /// <summary>
    /// Tenant the context is currently scoped to. Referenced by the global query filters (EF Core
    /// evaluates it as a per-request query parameter, never baking it into the cached model) and by
    /// <see cref="ApplyAuditAndConcurrency"/> when stamping new rows. Falls back to the default
    /// tenant for design-time/seed/test contexts that never resolve a tenant.
    /// </summary>
    public Guid CurrentTenantId => _tenant?.TenantId ?? TenantConstants.DefaultTenantId;

    /// <summary>Users table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Inventory items table.</summary>
    public DbSet<Inventory> Inventories => Set<Inventory>();

    /// <summary>Inventory assignments table.</summary>
    public DbSet<InventoryAssignment> InventoryAssignments => Set<InventoryAssignment>();

    /// <summary>Append-only stock-movement ledger table.</summary>
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    /// <summary>Managed suppliers/vendors table.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Managed storage locations table.</summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>Maintenance/calibration schedules table.</summary>
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();

    /// <summary>Dynamic, tenant-scoped roles table.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Tenant permission-catalog table.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Role → permission grants table.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>User → role assignments table.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Idempotency records table.</summary>
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyUniqueWhenPresentIndexes(modelBuilder);

        // Provider-agnostic cross-cutting model rules for every BaseEntity: an optimistic concurrency
        // token plus a single global query filter enforcing tenant isolation and soft-delete.
        // Centralizing the filter keeps it identical across all entities (avoiding required-navigation
        // filter warnings) and removes per-configuration duplication.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder
                .Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.ConcurrencyToken))
                .IsConcurrencyToken();

            SetGlobalQueryFilterMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, new object[] { modelBuilder });
        }
    }

    /// <summary>
    /// Adds the per-tenant "unique when present" constraints that the application layer cannot
    /// enforce safely on its own: an <c>IsBarcodeExistsAsync</c>/<c>IsSerialNumberExistsAsync</c>
    /// pre-check followed by an insert is a check-then-act race, so without a real constraint two
    /// concurrent creates both succeed and silently produce duplicate rows.
    /// <para>
    /// The constraint has to skip rows with no barcode/serial (many items legitimately have none, and
    /// a plain unique index would either reject them — SQL Server treats NULLs as equal — or collide
    /// on empty strings) and skip soft-deleted rows (so a barcode can be reused after deletion, which
    /// is what the soft-delete-aware pre-check already implies). That means a filtered/partial index,
    /// whose predicate is the one piece of SQL that cannot be written provider-agnostically; it is
    /// obtained from <see cref="IDatabaseProviderDialect"/> and applied here, where the configured
    /// provider is known. For an unrecognized provider no filter is available and the index is
    /// skipped, leaving the previous (application-only) behaviour untouched.
    /// </para>
    /// <para>
    /// These are separate, additionally named indexes rather than <c>IsUnique</c> on the existing
    /// composite lookup indexes in <see cref="Configurations.InventoryConfiguration"/>: a partial
    /// index whose predicate includes <c>&lt;&gt; ''</c> cannot be proven applicable for a plain
    /// <c>WHERE TenantId = @t AND Barcode = @b</c> lookup, so the unfiltered index is kept for reads
    /// while these enforce integrity.
    /// </para>
    /// </summary>
    private void ApplyUniqueWhenPresentIndexes(ModelBuilder modelBuilder)
    {
        var dialect = DatabaseProviderDialects.For(Database.ProviderName);

        AddUniqueWhenPresentIndex(
            nameof(Inventory.Barcode),
            "UX_Inventories_TenantId_Barcode",
            i => new { i.TenantId, i.Barcode }
        );

        AddUniqueWhenPresentIndex(
            nameof(Inventory.SerialNumber),
            "UX_Inventories_TenantId_SerialNumber",
            i => new { i.TenantId, i.SerialNumber }
        );

        void AddUniqueWhenPresentIndex(
            string valueColumn,
            string indexName,
            Expression<Func<Inventory, object?>> keySelector
        )
        {
            var filter = dialect.BuildUniqueWhenPresentFilter(
                valueColumn,
                nameof(BaseEntity.IsDeleted)
            );
            if (filter is null)
            {
                return;
            }

            modelBuilder
                .Entity<Inventory>()
                .HasIndex(keySelector, indexName)
                .IsUnique()
                .HasFilter(filter)
                .HasDatabaseName(indexName);
        }
    }

    private static readonly MethodInfo SetGlobalQueryFilterMethod = typeof(AppDbContext).GetMethod(
        nameof(SetTenantAndSoftDeleteFilter),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    /// <summary>
    /// Applies the combined tenant + soft-delete global query filter for <typeparamref name="TEntity"/>.
    /// <see cref="CurrentTenantId"/> is a context instance member, so EF Core translates it into a
    /// per-request query parameter (the officially supported multi-tenant pattern) rather than baking
    /// a value into the cached model. Uses only LINQ, so it is fully provider-agnostic.
    /// </summary>
    private void SetTenantAndSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity =>
        modelBuilder
            .Entity<TEntity>()
            .HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);

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
                    // Stamp the owning tenant on insert unless the caller set one explicitly.
                    if (entry.Entity.TenantId == Guid.Empty)
                    {
                        entry.Entity.TenantId = CurrentTenantId;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.ConcurrencyToken = Guid.NewGuid();
                    break;
            }
        }
    }
}
