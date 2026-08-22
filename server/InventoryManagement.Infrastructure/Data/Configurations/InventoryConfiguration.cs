using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Inventory"/>: constraints, unique filtered indexes and soft-delete filter.</summary>
public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");

        // Primary Key
        builder.HasKey(i => i.Id);

        // Properties
        builder.Property(i => i.EquipmentName).IsRequired().HasMaxLength(200);

        builder.Property(i => i.Description).HasMaxLength(1000);

        builder.Property(i => i.Category).HasMaxLength(100);

        builder.Property(i => i.Brand).HasMaxLength(100);

        builder.Property(i => i.Model).HasMaxLength(100);

        builder
            .Property(i => i.SerialNumber)
            .HasMaxLength(BusinessConstants.Inventory.MaxSerialNumberLength);

        builder.Property(i => i.Barcode).HasMaxLength(BusinessConstants.Inventory.MaxBarcodeLength);

        builder.Property(i => i.PurchasePrice).HasPrecision(18, 2);

        builder.Property(i => i.Supplier).HasMaxLength(200);

        builder.Property(i => i.Status).IsRequired().HasConversion<int>();

        builder.Property(i => i.Location).HasMaxLength(200);

        builder.Property(i => i.Notes).HasMaxLength(1000);

        // Auditing fields from BaseEntity
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.Property(i => i.CreatedBy).HasMaxLength(100);

        builder.Property(i => i.UpdatedBy).HasMaxLength(100);

        builder.Property(i => i.DeletedBy).HasMaxLength(100);

        // Indexes
        builder.HasIndex(i => i.EquipmentName).HasDatabaseName("IX_Inventories_EquipmentName");

        // Provider-agnostic composite lookups (tenant-scoped). Uniqueness "when present" for
        // barcode/serial is enforced per-tenant in the application layer (InventoryService), because a
        // partial/filtered unique index needs provider-specific raw SQL ("[col] IS NOT NULL"), which
        // is deliberately avoided so the model maps cleanly onto any SQL provider.
        builder
            .HasIndex(i => new { i.TenantId, i.Barcode })
            .HasDatabaseName("IX_Inventories_TenantId_Barcode");

        builder
            .HasIndex(i => new { i.TenantId, i.SerialNumber })
            .HasDatabaseName("IX_Inventories_TenantId_SerialNumber");

        builder.HasIndex(i => i.Category).HasDatabaseName("IX_Inventories_Category");

        builder.HasIndex(i => i.Status).HasDatabaseName("IX_Inventories_Status");

        builder.HasIndex(i => i.ExpiryDate).HasDatabaseName("IX_Inventories_ExpiryDate");

        builder.HasIndex(i => i.IsDeleted).HasDatabaseName("IX_Inventories_IsDeleted");

        builder.HasIndex(i => i.CreatedByUserId).HasDatabaseName("IX_Inventories_CreatedByUserId");

        builder.HasIndex(i => i.SupplierId).HasDatabaseName("IX_Inventories_SupplierId");

        builder.HasIndex(i => i.LocationId).HasDatabaseName("IX_Inventories_LocationId");

        // Relationships
        builder
            .HasMany(i => i.Assignments)
            .WithOne(ia => ia.Inventory)
            .HasForeignKey(ia => ia.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(i => i.CreatedByUser)
            .WithMany(u => u.CreatedInventories)
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional managed supplier/location references (additive to the legacy free-text columns).
        // Restrict so a supplier/location that is still referenced cannot be hard-deleted.
        builder
            .HasOne(i => i.SupplierEntity)
            .WithMany(s => s.Inventories)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(i => i.LocationEntity)
            .WithMany(l => l.Inventories)
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // The tenant + soft-delete global query filter is applied centrally in
        // AppDbContext.OnModelCreating for every BaseEntity (the composite tenant indexes above serve
        // tenant-scoped scans, so no standalone tenant index is needed here).
    }
}
