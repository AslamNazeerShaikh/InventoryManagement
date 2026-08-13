using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.Configuration;

/// <summary>
/// Configuration for the HTTP idempotency middleware, bound from the <c>Idempotency</c> section.
/// Controls key limits, response-cache bounds, lock/retention windows and excluded routes.
/// </summary>
public sealed class IdempotencyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Idempotency";

    /// <summary>Maximum accepted length of the <c>Idempotency-Key</c> header value.</summary>
    [Range(1, 512)]
    public int MaxKeyLength { get; set; } = 100;

    /// <summary>
    /// Maximum response body size (bytes) that will be buffered and cached for replay. Responses
    /// larger than this are streamed straight through and marked non-replayable to bound memory.
    /// </summary>
    [Range(1024, 10 * 1024 * 1024)]
    public int MaxCacheableBodyBytes { get; set; } = 256 * 1024;

    /// <summary>How long an in-progress lock is honoured before it is considered abandoned and reclaimable.</summary>
    [Range(typeof(TimeSpan), "00:00:05", "01:00:00")]
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How long a completed response remains replayable before it is eligible for purge.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan Retention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Interval at which the background cleanup service purges expired records.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Maximum rows deleted per cleanup batch (keeps purges cheap and non-blocking).</summary>
    [Range(10, 100_000)]
    public int CleanupBatchSize { get; set; } = 500;

    /// <summary>
    /// Request path prefixes (case-insensitive) excluded from idempotency handling. Auth endpoints
    /// are excluded by default so credential/token responses are never buffered or replayed.
    /// </summary>
    public string[] ExcludedPathPrefixes { get; set; } = new[] { "/api/auth" };
}
