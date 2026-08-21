using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core mapping for the append-only <see cref="StockMovement"/> ledger: value constraints,
/// query indexes, and optional relationships that preserve history (optional actors are set null on
/// delete, the required item reference is restricted). Soft-delete filter keeps it consistent with
/// its filtered principals.
/// </summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType).IsRequired().HasConversion<int>();

        builder.Property(m => m.QuantityChange).IsRequired();

        builder.Property(m => m.BalanceAfter).IsRequired();

        builder.Property(m => m.Reason).HasMaxLength(500);

        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.Property(m => m.UnitCost).HasColumnType("decimal(18,2)");

        builder.Property(m => m.CreatedAt).IsRequired();

        builder.Property(m => m.CreatedBy).HasMaxLength(100);

        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.Property(m => m.DeletedBy).HasMaxLength(100);

        // Indexes tuned for the two dominant reads: an item's timeline, and global recent activity.
        builder.HasIndex(m => m.InventoryId).HasDatabaseName("IX_StockMovements_InventoryId");

        builder.HasIndex(m => m.MovementType).HasDatabaseName("IX_StockMovements_MovementType");

        builder.HasIndex(m => m.CreatedAt).HasDatabaseName("IX_StockMovements_CreatedAt");

        builder
            .HasIndex(m => m.PerformedByUserId)
            .HasDatabaseName("IX_StockMovements_PerformedByUserId");

        builder.HasIndex(m => m.AssignmentId).HasDatabaseName("IX_StockMovements_AssignmentId");

        // Required item reference: restrict so history is never orphaned by an item delete.
        builder
            .HasOne(m => m.Inventory)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(m => m.InventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional references: null out on delete so a movement row always survives (audit integrity).
        builder
            .HasOne(m => m.PerformedByUser)
            .WithMany()
            .HasForeignKey(m => m.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(m => m.Assignment)
            .WithMany()
            .HasForeignKey(m => m.AssignmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(m => m.FromLocation)
            .WithMany()
            .HasForeignKey(m => m.FromLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(m => m.ToLocation)
            .WithMany()
            .HasForeignKey(m => m.ToLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(m => m.Supplier)
            .WithMany()
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
