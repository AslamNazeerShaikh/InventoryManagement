using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Supplier"/>: value constraints, a unique-name filtered index and soft-delete filter.</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);

        builder.Property(s => s.ContactName).HasMaxLength(200);

        builder.Property(s => s.Email).HasMaxLength(256);

        builder.Property(s => s.Phone).HasMaxLength(50);

        builder.Property(s => s.Address).HasMaxLength(500);

        builder.Property(s => s.Website).HasMaxLength(500);

        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.CreatedAt).IsRequired();

        builder.Property(s => s.CreatedBy).HasMaxLength(100);

        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.Property(s => s.DeletedBy).HasMaxLength(100);

        // Unique supplier name among non-deleted rows (case-insensitive collation handled by provider).
        builder
            .HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_Suppliers_Name")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => s.IsActive).HasDatabaseName("IX_Suppliers_IsActive");

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
