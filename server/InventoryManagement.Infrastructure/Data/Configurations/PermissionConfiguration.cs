using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Permission"/>. Codes are unique per tenant.</summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);
        builder.Property(p => p.DeletedBy).HasMaxLength(100);

        // Unique permission code per tenant (Code is required → provider-safe composite unique).
        builder
            .HasIndex(p => new { p.TenantId, p.Code })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_TenantId_Code");
    }
}
