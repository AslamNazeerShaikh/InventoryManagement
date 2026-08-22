using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for the <see cref="UserRole"/> join. A user holds each role at most once.</summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.CreatedBy).HasMaxLength(100);
        builder.Property(ur => ur.UpdatedBy).HasMaxLength(100);
        builder.Property(ur => ur.DeletedBy).HasMaxLength(100);

        builder
            .HasIndex(ur => new { ur.TenantId, ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_TenantId_UserId_RoleId");

        builder.HasIndex(ur => ur.RoleId).HasDatabaseName("IX_UserRoles_RoleId");

        // Deleting a user removes their memberships; a role that still has members cannot be deleted
        // (single cascade path per table keeps the schema valid on every provider, e.g. SQL Server).
        builder
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
