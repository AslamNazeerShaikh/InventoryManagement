using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

public class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.ToTable("IdempotentRequests");

        // Primary Key
        builder.HasKey(x => x.IdempotencyKey);

        // Properties
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100);

        builder.Property(x => x.RequestMethod).IsRequired().HasMaxLength(10);

        builder.Property(x => x.RequestPath).IsRequired().HasMaxLength(500);

        builder.Property(x => x.ResponseContentType).HasMaxLength(100);

        builder.Property(x => x.CreatedAt).IsRequired();

        // Indexes
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_IdempotentRequests_CreatedAt");
    }
}
