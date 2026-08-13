using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.Security;
using InventoryManagement.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>A settable clock for deterministic time-dependent tests.</summary>
internal sealed class MutableClock : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>Stub secret client (unused for inline keys).</summary>
internal sealed class StubSecretClient : ISecretClient
{
    public Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default
    ) => Task.FromResult<string?>(null);
}

public class PasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesValueDifferentFromInput()
    {
        var hash = _hasher.Hash("Sup3r$ecret!");
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("Sup3r$ecret!", hash);
    }

    [Fact]
    public void Verify_ReturnsSuccess_ForCorrectPassword()
    {
        var hash = _hasher.Hash("Sup3r$ecret!");
        Assert.Equal(PasswordVerificationOutcome.Success, _hasher.Verify(hash, "Sup3r$ecret!"));
    }

    [Fact]
    public void Verify_ReturnsFailed_ForWrongPassword()
    {
        var hash = _hasher.Hash("Sup3r$ecret!");
        Assert.Equal(PasswordVerificationOutcome.Failed, _hasher.Verify(hash, "wrong"));
    }

    [Fact]
    public void Hash_IsSaltedSoTwoHashesDiffer()
    {
        Assert.NotEqual(_hasher.Hash("same"), _hasher.Hash("same"));
    }
}

public class TokenServiceTests
{
    private static TokenService CreateService(MutableClock clock)
    {
        var options = Options.Create(
            new JwtOptions
            {
                KeySource = JwtKeySource.Inline,
                Key = "unit-test-signing-key-that-is-at-least-32-bytes-long!!",
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenMinutes = 30,
                RefreshTokenDays = 5,
            }
        );
        var keyProvider = new JwtSigningKeyProvider(
            options,
            new StubSecretClient(),
            NullLogger<JwtSigningKeyProvider>.Instance
        );
        return new TokenService(keyProvider, clock, options);
    }

    [Fact]
    public async Task CreateAccessToken_ReturnsSignedJwtWithExpiry()
    {
        var clock = new MutableClock();
        var service = CreateService(clock);

        var token = await service.CreateAccessTokenAsync(
            new Domain.DTOs.UserDto
            {
                Id = 1,
                Email = "a@b.com",
                Name = "A",
            }
        );

        Assert.False(string.IsNullOrWhiteSpace(token.Value));
        Assert.Equal(3, token.Value.Split('.').Length); // header.payload.signature
        Assert.Equal(clock.UtcNow.AddMinutes(30), token.ExpiresAtUtc);
    }

    [Fact]
    public void CreateRefreshToken_HashMatchesDeterministicHashOfRawValue()
    {
        var service = CreateService(new MutableClock());

        var refresh = service.CreateRefreshToken();

        Assert.Equal(refresh.Hash, service.HashRefreshToken(refresh.RawValue));
        Assert.NotEqual(refresh.RawValue, refresh.Hash);
    }

    [Fact]
    public void CreateRefreshToken_ProducesUniqueValues()
    {
        var service = CreateService(new MutableClock());
        Assert.NotEqual(
            service.CreateRefreshToken().RawValue,
            service.CreateRefreshToken().RawValue
        );
    }
}
