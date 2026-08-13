using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Data.Configurations;

/// <summary>EF Core mapping for <see cref="IdempotentRequest"/>, including bounded response storage and purge indexes.</summary>
public class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.ToTable("IdempotentRequests");

        builder.HasKey(x => x.IdempotencyKey);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(512);

        builder.Property(x => x.RequestMethod).IsRequired().HasMaxLength(10);

        builder.Property(x => x.RequestPath).IsRequired().HasMaxLength(500);

        builder.Property(x => x.RequestHash).HasMaxLength(64);

        builder.Property(x => x.ResponseContentType).HasMaxLength(100);

        // Bound cached responses at the storage layer as defence-in-depth against oversized rows.
        builder.Property(x => x.ResponseBody).HasMaxLength(1024 * 1024);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.Property(x => x.LockExpiresAt).IsRequired();

        // Indexes supporting lock-expiry reclaim and retention-based purging.
        builder
            .HasIndex(x => x.LockExpiresAt)
            .HasDatabaseName("IX_IdempotentRequests_LockExpiresAt");

        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_IdempotentRequests_ExpiresAt");
    }
}
