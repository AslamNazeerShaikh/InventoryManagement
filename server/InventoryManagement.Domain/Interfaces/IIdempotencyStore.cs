using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Outcome of attempting to begin an idempotent operation for a given key.</summary>
public enum IdempotencyBeginStatus
{
    /// <summary>The caller acquired the lock and should execute the operation.</summary>
    Proceed = 0,

    /// <summary>Another in-flight request holds the lock; the caller should return HTTP 409.</summary>
    InProgress = 1,

    /// <summary>A completed response exists and should be replayed to the caller.</summary>
    Replay = 2,

    /// <summary>The key was previously used for a different method/path/body; reject as a collision.</summary>
    KeyMismatch = 3,
}

/// <summary>Result of <see cref="IIdempotencyStore.TryBeginAsync"/>.</summary>
/// <param name="Status">The begin outcome.</param>
/// <param name="Record">The persisted record when <see cref="Status"/> is <c>Replay</c>; otherwise <c>null</c>.</param>
public readonly record struct IdempotencyBeginResult(
    IdempotencyBeginStatus Status,
    IdempotentRequest? Record
);

/// <summary>
/// Persistence-backed idempotency coordinator. Provides atomic lock acquisition (with reclaim of
/// abandoned/expired locks), completion capture for replay, lock release on failure, and bounded
/// retention purging. Abstracts the storage layer away from the HTTP middleware.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Attempts to begin an idempotent operation: inserts an in-progress lock, replays a completed
    /// response, reports an in-progress conflict, reclaims an abandoned lock, or flags a key/payload
    /// collision — all atomically.
    /// </summary>
    /// <param name="key">The client idempotency key.</param>
    /// <param name="method">HTTP method of the current request.</param>
    /// <param name="path">Request path of the current request.</param>
    /// <param name="requestHash">Hash of the request body used to detect key/payload collisions.</param>
    /// <param name="lockDuration">How long the acquired lock is honoured.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IdempotencyBeginResult> TryBeginAsync(
        string key,
        string method,
        string path,
        string? requestHash,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>Captures a completed response for future replay and sets its retention deadline.</summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="statusCode">Captured response status code.</param>
    /// <param name="contentType">Captured response content type.</param>
    /// <param name="body">Captured response body, or <c>null</c> if too large to cache.</param>
    /// <param name="retention">How long the completed response remains replayable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteAsync(
        string key,
        int statusCode,
        string? contentType,
        string? body,
        TimeSpan retention,
        CancellationToken cancellationToken = default
    );

    /// <summary>Releases (deletes) the lock for a key so the operation can be safely retried.</summary>
    Task ReleaseAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes up to <paramref name="batchSize"/> expired records; returns the number removed.</summary>
    Task<int> PurgeExpiredAsync(int batchSize, CancellationToken cancellationToken = default);
}
