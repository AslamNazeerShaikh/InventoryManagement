using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Idempotency;

/// <summary>
/// EF Core–backed <see cref="IIdempotencyStore"/>. Correctness relies on the primary-key uniqueness
/// of the idempotency key plus short transactions; concurrent inserts for the same key resolve to a
/// single winner while the loser observes an in-progress conflict.
/// </summary>
public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly AppDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<EfIdempotencyStore> _logger;

    /// <summary>Creates the store.</summary>
    public EfIdempotencyStore(
        AppDbContext dbContext,
        IDateTimeProvider clock,
        ILogger<EfIdempotencyStore> logger
    )
    {
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IdempotencyBeginResult> TryBeginAsync(
        string key,
        string method,
        string path,
        string? requestHash,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default
    )
    {
        var now = _clock.UtcNow;
        var existing = await _dbContext
            .IdempotentRequests.FirstOrDefaultAsync(r => r.IdempotencyKey == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.IsCompleted)
            {
                // Retention still valid → detect collision, otherwise replay.
                if (existing.ExpiresAt is null || existing.ExpiresAt > now)
                {
                    if (IsSameRequest(existing, method, path, requestHash))
                    {
                        return new IdempotencyBeginResult(IdempotencyBeginStatus.Replay, existing);
                    }

                    return new IdempotencyBeginResult(IdempotencyBeginStatus.KeyMismatch, null);
                }

                // Retention elapsed → reclaim the row as a fresh in-progress lock.
                Reclaim(existing, method, path, requestHash, now, lockDuration);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new IdempotencyBeginResult(IdempotencyBeginStatus.Proceed, null);
            }

            // In-progress: honour the lock unless it has expired (abandoned by a crashed request).
            if (existing.LockExpiresAt > now)
            {
                return new IdempotencyBeginResult(IdempotencyBeginStatus.InProgress, null);
            }

            _logger.LogWarning(
                "Reclaiming expired idempotency lock for key {IdempotencyKey}.",
                key
            );
            Reclaim(existing, method, path, requestHash, now, lockDuration);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new IdempotencyBeginResult(IdempotencyBeginStatus.Proceed, null);
        }

        // No record: attempt to insert a new in-progress lock. A concurrent inserter may win the
        // race, in which case the unique primary key surfaces a DbUpdateException we treat as 409.
        var record = new IdempotentRequest
        {
            IdempotencyKey = key,
            RequestMethod = method,
            RequestPath = path,
            RequestHash = requestHash,
            CreatedAt = now,
            LockExpiresAt = now.Add(lockDuration),
            IsCompleted = false,
        };

        try
        {
            await _dbContext
                .IdempotentRequests.AddAsync(record, cancellationToken)
                .ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new IdempotencyBeginResult(IdempotencyBeginStatus.Proceed, null);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(record).State = EntityState.Detached;
            return new IdempotencyBeginResult(IdempotencyBeginStatus.InProgress, null);
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync(
        string key,
        int statusCode,
        string? contentType,
        string? body,
        TimeSpan retention,
        CancellationToken cancellationToken = default
    )
    {
        var record = await _dbContext
            .IdempotentRequests.FirstOrDefaultAsync(r => r.IdempotencyKey == key, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        record.ResponseStatusCode = statusCode;
        record.ResponseContentType = contentType;
        record.ResponseBody = body;
        record.IsCompleted = true;
        record.CompletedAt = now;
        record.ExpiresAt = now.Add(retention);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        // Set-based delete avoids loading the entity; no-op when already gone.
        await _dbContext
            .IdempotentRequests.Where(r => r.IdempotencyKey == key)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> PurgeExpiredAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var now = _clock.UtcNow;

        // Purge completed rows past retention and abandoned in-progress locks past their lock window.
        var expiredKeys = await _dbContext
            .IdempotentRequests.AsNoTracking()
            .Where(r =>
                (r.IsCompleted && r.ExpiresAt != null && r.ExpiresAt < now)
                || (!r.IsCompleted && r.LockExpiresAt < now)
            )
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.IdempotencyKey)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredKeys.Count == 0)
        {
            return 0;
        }

        return await _dbContext
            .IdempotentRequests.Where(r => expiredKeys.Contains(r.IdempotencyKey))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool IsSameRequest(
        IdempotentRequest record,
        string method,
        string path,
        string? requestHash
    ) =>
        string.Equals(record.RequestMethod, method, StringComparison.Ordinal)
        && string.Equals(record.RequestPath, path, StringComparison.Ordinal)
        && string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal);

    private static void Reclaim(
        IdempotentRequest record,
        string method,
        string path,
        string? requestHash,
        DateTime now,
        TimeSpan lockDuration
    )
    {
        record.RequestMethod = method;
        record.RequestPath = path;
        record.RequestHash = requestHash;
        record.ResponseBody = null;
        record.ResponseContentType = null;
        record.ResponseStatusCode = 0;
        record.IsCompleted = false;
        record.CompletedAt = null;
        record.ExpiresAt = null;
        record.CreatedAt = now;
        record.LockExpiresAt = now.Add(lockDuration);
    }
}
