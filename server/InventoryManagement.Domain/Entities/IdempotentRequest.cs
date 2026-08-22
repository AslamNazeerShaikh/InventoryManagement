namespace InventoryManagement.Domain.Entities;

/// <summary>
/// Persisted record backing HTTP idempotency. One row per <c>Idempotency-Key</c> guards a
/// state-changing request so retries replay the original outcome instead of re-executing it.
/// </summary>
/// <remarks>
/// Lifecycle: a row is inserted <em>in-progress</em> (acts as a distributed lock), then either
/// completed (response captured for replay) or removed (on server error) to allow a safe retry.
/// The <see cref="LockExpiresAt"/> and <see cref="ExpiresAt"/> stamps bound how long a lock is
/// honoured and how long a completed response remains replayable.
/// </remarks>
public class IdempotentRequest
{
    /// <summary>Client-supplied idempotency key (primary key). Max length enforced by configuration.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>HTTP method of the original request, used to detect key reuse across verbs.</summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>Request path of the original request, used to detect key reuse across routes.</summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>SHA-256 hash (hex) of the original request body, used to reject key/payload collisions.</summary>
    public string? RequestHash { get; set; }

    /// <summary>Captured HTTP status code of the completed response (0 while in-progress).</summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>Captured response body for replay. <c>null</c> when the body exceeded the cache limit.</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Captured response content type for replay.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>UTC timestamp when the in-progress lock row was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the response was captured and the request marked complete.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// UTC deadline for the in-progress lock. A non-completed row past this instant is treated as an
    /// abandoned lock (e.g. process crash) and may be reclaimed by a new request.
    /// </summary>
    public DateTime LockExpiresAt { get; set; }

    /// <summary>UTC instant after which a completed record is no longer replayable and can be purged.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary><c>true</c> once the response has been captured and is available for replay.</summary>
    public bool IsCompleted { get; set; }
}
