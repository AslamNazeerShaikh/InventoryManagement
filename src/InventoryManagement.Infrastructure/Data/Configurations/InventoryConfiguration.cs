using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
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

        builder.Property(i => i.PurchasePrice).HasColumnType("decimal(18,2)");

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

        builder
            .HasIndex(i => i.Barcode)
            .IsUnique()
            .HasDatabaseName("IX_Inventories_Barcode")
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder
            .HasIndex(i => i.SerialNumber)
            .IsUnique()
            .HasDatabaseName("IX_Inventories_SerialNumber")
            .HasFilter("[SerialNumber] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(i => i.Category).HasDatabaseName("IX_Inventories_Category");

        builder.HasIndex(i => i.Status).HasDatabaseName("IX_Inventories_Status");

        builder.HasIndex(i => i.ExpiryDate).HasDatabaseName("IX_Inventories_ExpiryDate");

        builder.HasIndex(i => i.IsDeleted).HasDatabaseName("IX_Inventories_IsDeleted");

        builder.HasIndex(i => i.CreatedByUserId).HasDatabaseName("IX_Inventories_CreatedByUserId");

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

        // Query filters for soft delete
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
