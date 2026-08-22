using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="MaintenanceSchedule"/>: value constraints, due-date/status indexes, relationships and soft-delete filter.</summary>
public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
    {
        builder.ToTable("MaintenanceSchedules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MaintenanceType).IsRequired().HasConversion<int>();

        builder.Property(m => m.Status).IsRequired().HasConversion<int>();

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);

        builder.Property(m => m.Description).HasMaxLength(1000);

        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.Property(m => m.NextDueAt).IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();

        builder.Property(m => m.CreatedBy).HasMaxLength(100);

        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.Property(m => m.DeletedBy).HasMaxLength(100);

        builder
            .HasIndex(m => m.InventoryId)
            .HasDatabaseName("IX_MaintenanceSchedules_InventoryId");

        builder.HasIndex(m => m.NextDueAt).HasDatabaseName("IX_MaintenanceSchedules_NextDueAt");

        builder.HasIndex(m => m.Status).HasDatabaseName("IX_MaintenanceSchedules_Status");

        builder
            .HasOne(m => m.Inventory)
            .WithMany(i => i.MaintenanceSchedules)
            .HasForeignKey(m => m.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(m => m.PerformedByUser)
            .WithMany()
            .HasForeignKey(m => m.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Tenant isolation index; the tenant + soft-delete global filter is applied centrally in
        // AppDbContext.OnModelCreating for every BaseEntity.
        builder.HasIndex(m => m.TenantId).HasDatabaseName("IX_MaintenanceSchedules_TenantId");
    }
}
