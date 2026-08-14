using System.Security.Cryptography;
using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryManagement.API.Infrastructure;

/// <summary>
/// HTTP idempotency middleware for state-changing requests carrying an <c>Idempotency-Key</c> header.
/// Delegates persistence/locking to <see cref="IIdempotencyStore"/>. To capture the outcome for
/// replay it buffers the response into memory; only responses whose size is within
/// <see cref="IdempotencyOptions.MaxCacheableBodyBytes"/> are persisted for replay (larger responses
/// still complete normally but are marked non-replayable). It hashes the request body to detect
/// key/payload collisions, releases the lock on server errors to permit safe retries, and skips
/// configured (e.g. authentication) paths so sensitive responses are never buffered or replayed.
/// Registered after authentication so anonymous callers cannot populate the store.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    private readonly RequestDelegate _next;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    /// <summary>Creates the middleware.</summary>
    public IdempotencyMiddleware(
        RequestDelegate next,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyMiddleware> logger
    )
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Processes the request through the idempotency protocol.</summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="store">Scoped idempotency store resolved per request.</param>
    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
    {
        if (!ShouldHandle(context, out var idempotencyKey))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (idempotencyKey.Length > _options.MaxKeyLength)
        {
            await WriteAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    $"Idempotency key exceeds the maximum length of {_options.MaxKeyLength} characters."
                )
                .ConfigureAwait(false);
            return;
        }

        var requestHash = await ComputeRequestHashAsync(context).ConfigureAwait(false);

        var begin = await store
            .TryBeginAsync(
                idempotencyKey,
                context.Request.Method,
                context.Request.Path.ToString(),
                requestHash,
                _options.LockDuration,
                context.RequestAborted
            )
            .ConfigureAwait(false);

        switch (begin.Status)
        {
            case IdempotencyBeginStatus.InProgress:
                await WriteAsync(
                        context,
                        StatusCodes.Status409Conflict,
                        "A request with this idempotency key is already in progress."
                    )
                    .ConfigureAwait(false);
                return;

            case IdempotencyBeginStatus.KeyMismatch:
                await WriteAsync(
                        context,
                        StatusCodes.Status422UnprocessableEntity,
                        "This idempotency key was already used for a different request."
                    )
                    .ConfigureAwait(false);
                return;

            case IdempotencyBeginStatus.Replay:
                await ReplayAsync(context, begin.Record!).ConfigureAwait(false);
                return;
        }

        await ExecuteAndCaptureAsync(context, store, idempotencyKey).ConfigureAwait(false);
    }

    private bool ShouldHandle(HttpContext context, out string idempotencyKey)
    {
        idempotencyKey = string.Empty;

        if (!MutatingMethods.Contains(context.Request.Method))
        {
            return false;
        }

        if (
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var header)
            || string.IsNullOrWhiteSpace(header)
        )
        {
            return false;
        }

        var path = context.Request.Path.ToString();
        foreach (var prefix in _options.ExcludedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        idempotencyKey = header.ToString().Trim();
        return idempotencyKey.Length > 0;
    }

    /// <summary>Buffers the response up to the configured cap, capturing it for replay when small enough.</summary>
    private async Task ExecuteAndCaptureAsync(
        HttpContext context,
        IIdempotencyStore store,
        string idempotencyKey
    )
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);

            var statusCode = context.Response.StatusCode;

            // Server errors release the lock so the operation can be retried safely.
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                await store
                    .ReleaseAsync(idempotencyKey, context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else
            {
                string? body = null;
                if (buffer.Length <= _options.MaxCacheableBodyBytes)
                {
                    body = System.Text.Encoding.UTF8.GetString(
                        buffer.GetBuffer(),
                        0,
                        (int)buffer.Length
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Response for idempotency key {IdempotencyKey} ({Bytes} bytes) exceeds the "
                            + "cache limit; it will not be replayable.",
                        idempotencyKey,
                        buffer.Length
                    );
                }

                await store
                    .CompleteAsync(
                        idempotencyKey,
                        statusCode,
                        context.Response.ContentType,
                        body,
                        _options.Retention,
                        context.RequestAborted
                    )
                    .ConfigureAwait(false);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
        }
        catch
        {
            // Unhandled failure: release the lock so a retry is possible, then rethrow.
            await SafeReleaseAsync(store, idempotencyKey).ConfigureAwait(false);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private async Task SafeReleaseAsync(IIdempotencyStore store, string idempotencyKey)
    {
        try
        {
            await store.ReleaseAsync(idempotencyKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to release idempotency lock {IdempotencyKey} after a failure.",
                idempotencyKey
            );
        }
    }

    private static async Task ReplayAsync(
        HttpContext context,
        Domain.Entities.IdempotentRequest record
    )
    {
        context.Response.StatusCode = record.ResponseStatusCode;
        context.Response.ContentType = record.ResponseContentType ?? "application/json";
        context.Response.Headers["Idempotency-Replayed"] = "true";
        if (!string.IsNullOrEmpty(record.ResponseBody))
        {
            await context
                .Response.WriteAsync(record.ResponseBody, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Computes a SHA-256 hash of the (rewindable) request body for collision detection.</summary>
    private static async Task<string?> ComputeRequestHashAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        if (context.Request.ContentLength is null or 0)
        {
            return null;
        }

        context.Request.Body.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(context.Request.Body, context.RequestAborted)
            .ConfigureAwait(false);
        context.Request.Body.Position = 0;
        return Convert.ToHexString(hash);
    }

    private static Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(message));
    }
}
