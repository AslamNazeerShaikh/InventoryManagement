using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Role"/>. Role names are unique per tenant.</summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);
        builder.Property(r => r.DeletedBy).HasMaxLength(100);

        // Unique role name per tenant (Name is required, so an unfiltered composite unique index is
        // valid on every provider). The tenant + soft-delete global filter is applied centrally.
        builder
            .HasIndex(r => new { r.TenantId, r.Name })
            .IsUnique()
            .HasDatabaseName("IX_Roles_TenantId_Name");
    }
}
