using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="Location"/>: value constraints, a self-referencing hierarchy, a unique-code filtered index and soft-delete filter.</summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);

        builder.Property(l => l.Code).HasMaxLength(100);

        builder.Property(l => l.Description).HasMaxLength(1000);

        builder.Property(l => l.CreatedAt).IsRequired();

        builder.Property(l => l.CreatedBy).HasMaxLength(100);

        builder.Property(l => l.UpdatedBy).HasMaxLength(100);

        builder.Property(l => l.DeletedBy).HasMaxLength(100);

        // Unique location code among non-deleted rows that actually have a code.
        builder
            .HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("IX_Locations_Code")
            .HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(l => l.ParentLocationId).HasDatabaseName("IX_Locations_ParentLocationId");

        builder.HasIndex(l => l.IsActive).HasDatabaseName("IX_Locations_IsActive");

        // Self-referencing hierarchy (site -> room -> shelf -> bin). Restrict so a parent with
        // children cannot be removed out from under them.
        builder
            .HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
