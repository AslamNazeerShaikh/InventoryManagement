using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Idempotency;

/// <summary>
/// Background worker that periodically purges expired idempotency records (completed rows past
/// retention and abandoned locks past their window). Runs in bounded batches within a fresh DI
/// scope, never throws out of the loop, and stops promptly on host shutdown.
/// </summary>
public sealed class IdempotencyCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyCleanupService> _logger;

    /// <summary>Creates the cleanup service.</summary>
    public IdempotencyCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<IdempotencyOptions> options,
        ILogger<IdempotencyCleanupService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await PurgeAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a transient failure tear down the host; retry next tick.
                _logger.LogError(ex, "Idempotency cleanup cycle failed; will retry next interval.");
            }
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

        int totalRemoved = 0;
        int removed;
        do
        {
            removed = await store
                .PurgeExpiredAsync(_options.CleanupBatchSize, cancellationToken)
                .ConfigureAwait(false);
            totalRemoved += removed;
        } while (
            removed == _options.CleanupBatchSize && !cancellationToken.IsCancellationRequested
        );

        if (totalRemoved > 0)
        {
            _logger.LogInformation("Purged {Count} expired idempotency record(s).", totalRemoved);
        }
    }
}
