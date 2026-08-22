using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for the <see cref="RolePermission"/> join. A permission is granted once per role.</summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.CreatedBy).HasMaxLength(100);
        builder.Property(rp => rp.UpdatedBy).HasMaxLength(100);
        builder.Property(rp => rp.DeletedBy).HasMaxLength(100);

        builder
            .HasIndex(rp => new { rp.TenantId, rp.RoleId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_TenantId_RoleId_PermissionId");

        builder.HasIndex(rp => rp.PermissionId).HasDatabaseName("IX_RolePermissions_PermissionId");

        // Deleting a role removes its grants; a permission cannot be removed while still granted.
        builder
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
