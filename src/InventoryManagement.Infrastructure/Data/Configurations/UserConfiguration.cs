using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Primary Key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);

        builder.Property(u => u.Role).IsRequired().HasConversion<int>();

        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        // Auditing fields from BaseEntity
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.Property(u => u.CreatedBy).HasMaxLength(100);

        builder.Property(u => u.UpdatedBy).HasMaxLength(100);

        builder.Property(u => u.DeletedBy).HasMaxLength(100);

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsDeleted).HasDatabaseName("IX_Users_IsDeleted");

        builder.HasIndex(u => u.IsActive).HasDatabaseName("IX_Users_IsActive");

        // Relationships
        builder
            .HasMany(u => u.AssignedInventories)
            .WithOne(ia => ia.User)
            .HasForeignKey(ia => ia.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(u => u.CreatedInventories)
            .WithOne(i => i.CreatedByUser)
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Query filters for soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
