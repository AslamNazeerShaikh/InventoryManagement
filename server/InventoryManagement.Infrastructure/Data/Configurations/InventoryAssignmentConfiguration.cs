using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="InventoryAssignment"/>: constraints, indexes, relationships and soft-delete filter.</summary>
public class InventoryAssignmentConfiguration : IEntityTypeConfiguration<InventoryAssignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InventoryAssignment> builder)
    {
        builder.ToTable("InventoryAssignments");

        // Primary Key
        builder.HasKey(ia => ia.Id);

        // Properties
        builder.Property(ia => ia.AssignedQuantity).IsRequired().HasDefaultValue(1);

        builder.Property(ia => ia.AssignedDate).IsRequired();

        builder.Property(ia => ia.Status).IsRequired().HasConversion<int>();

        builder.Property(ia => ia.AssignmentNotes).HasMaxLength(1000);

        builder.Property(ia => ia.ReturnNotes).HasMaxLength(1000);

        builder.Property(ia => ia.ReturnedQuantity).IsRequired().HasDefaultValue(0);

        builder.Property(ia => ia.RenewalCount).IsRequired().HasDefaultValue(0);

        builder.Property(ia => ia.ReturnCondition).HasConversion<int>();

        // Auditing fields from BaseEntity
        builder.Property(ia => ia.CreatedAt).IsRequired();

        builder.Property(ia => ia.CreatedBy).HasMaxLength(100);

        builder.Property(ia => ia.UpdatedBy).HasMaxLength(100);

        builder.Property(ia => ia.DeletedBy).HasMaxLength(100);

        // Indexes
        builder
            .HasIndex(ia => ia.InventoryId)
            .HasDatabaseName("IX_InventoryAssignments_InventoryId");

        builder.HasIndex(ia => ia.UserId).HasDatabaseName("IX_InventoryAssignments_UserId");

        builder.HasIndex(ia => ia.Status).HasDatabaseName("IX_InventoryAssignments_Status");

        builder
            .HasIndex(ia => ia.AssignedDate)
            .HasDatabaseName("IX_InventoryAssignments_AssignedDate");

        builder.HasIndex(ia => ia.ReturnDate).HasDatabaseName("IX_InventoryAssignments_ReturnDate");

        builder
            .HasIndex(ia => ia.ExpectedReturnDate)
            .HasDatabaseName("IX_InventoryAssignments_ExpectedReturnDate");

        builder.HasIndex(ia => ia.IsDeleted).HasDatabaseName("IX_InventoryAssignments_IsDeleted");

        builder
            .HasIndex(ia => ia.AssignedByUserId)
            .HasDatabaseName("IX_InventoryAssignments_AssignedByUserId");

        builder
            .HasIndex(ia => ia.ReturnedToUserId)
            .HasDatabaseName("IX_InventoryAssignments_ReturnedToUserId");

        // Relationships
        builder
            .HasOne(ia => ia.Inventory)
            .WithMany(i => i.Assignments)
            .HasForeignKey(ia => ia.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ia => ia.User)
            .WithMany(u => u.AssignedInventories)
            .HasForeignKey(ia => ia.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(ia => ia.AssignedByUser)
            .WithMany()
            .HasForeignKey(ia => ia.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(ia => ia.ReturnedToUser)
            .WithMany()
            .HasForeignKey(ia => ia.ReturnedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Query filters for soft delete
        builder.HasQueryFilter(ia => !ia.IsDeleted);
    }
}
