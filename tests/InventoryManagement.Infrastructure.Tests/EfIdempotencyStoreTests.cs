using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Idempotency;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>Tests for the EF-backed idempotency store: locking, replay, conflict and reclaim.</summary>
public class EfIdempotencyStoreTests
{
    private static readonly TimeSpan Lock = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Retention = TimeSpan.FromHours(1);

    private static EfIdempotencyStore CreateStore(SqliteInMemoryFixture fixture, MutableClock clock) =>
        new(fixture.Context, clock, NullLogger<EfIdempotencyStore>.Instance);

    [Fact]
    public async Task TryBegin_FirstCall_Proceeds()
    {
        using var fixture = new SqliteInMemoryFixture();
        var store = CreateStore(fixture, new MutableClock());

        var result = await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);

        Assert.Equal(IdempotencyBeginStatus.Proceed, result.Status);
    }

    [Fact]
    public async Task TryBegin_WhileInProgress_ReportsInProgress()
    {
        using var fixture = new SqliteInMemoryFixture();
        var store = CreateStore(fixture, new MutableClock());

        await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);
        var second = await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);

        Assert.Equal(IdempotencyBeginStatus.InProgress, second.Status);
    }

    [Fact]
    public async Task Complete_ThenTryBegin_ReplaysStoredResponse()
    {
        using var fixture = new SqliteInMemoryFixture();
        var store = CreateStore(fixture, new MutableClock());

        await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);
        await store.CompleteAsync("k1", 201, "application/json", "{\"ok\":true}", Retention);

        var replay = await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);

        Assert.Equal(IdempotencyBeginStatus.Replay, replay.Status);
        Assert.NotNull(replay.Record);
        Assert.Equal(201, replay.Record!.ResponseStatusCode);
        Assert.Equal("{\"ok\":true}", replay.Record.ResponseBody);
    }

    [Fact]
    public async Task Complete_ThenTryBeginWithDifferentRequest_ReportsMismatch()
    {
        using var fixture = new SqliteInMemoryFixture();
        var store = CreateStore(fixture, new MutableClock());

        await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);
        await store.CompleteAsync("k1", 200, "application/json", "{}", Retention);

        var mismatch = await store.TryBeginAsync("k1", "PUT", "/api/y", "other", Lock);

        Assert.Equal(IdempotencyBeginStatus.KeyMismatch, mismatch.Status);
    }

    [Fact]
    public async Task TryBegin_AfterLockExpires_ReclaimsAndProceeds()
    {
        using var fixture = new SqliteInMemoryFixture();
        var clock = new MutableClock();
        var store = CreateStore(fixture, clock);

        await store.TryBeginAsync("k1", "POST", "/api/x", "h", TimeSpan.FromMinutes(1));
        clock.UtcNow = clock.UtcNow.AddMinutes(5); // lock window elapsed

        var reclaimed = await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);

        Assert.Equal(IdempotencyBeginStatus.Proceed, reclaimed.Status);
    }

    [Fact]
    public async Task PurgeExpired_RemovesCompletedRecordsPastRetention()
    {
        using var fixture = new SqliteInMemoryFixture();
        var clock = new MutableClock();
        var store = CreateStore(fixture, clock);

        await store.TryBeginAsync("k1", "POST", "/api/x", "h", Lock);
        await store.CompleteAsync("k1", 200, "application/json", "{}", TimeSpan.FromMinutes(1));
        clock.UtcNow = clock.UtcNow.AddMinutes(5); // retention elapsed

        var removed = await store.PurgeExpiredAsync(100);

        Assert.Equal(1, removed);
    }
}
