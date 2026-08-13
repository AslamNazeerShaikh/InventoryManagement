using System.Text;
using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Infrastructure.Security;

/// <summary>
/// Resolves and caches the symmetric JWT signing key from the configured source (inline value,
/// environment variable, mounted file, or cloud secret). The resolved key is the single source of
/// truth shared by token <em>generation</em> and <em>validation</em>. Resolution is performed once
/// and cached; the key is validated to be at least 256 bits to satisfy HMAC-SHA256.
/// </summary>
public interface IJwtSigningKeyProvider
{
    /// <summary>Returns credentials (key + algorithm) used to sign newly issued access tokens.</summary>
    Task<SigningCredentials> GetSigningCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the symmetric key used to validate incoming access tokens.</summary>
    Task<SecurityKey> GetValidationKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>Default <see cref="IJwtSigningKeyProvider"/> with thread-safe, once-only key resolution.</summary>
public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private const int MinimumKeyBytes = 32; // 256 bits for HMAC-SHA256.

    private readonly JwtOptions _options;
    private readonly ISecretClient _secretClient;
    private readonly ILogger<JwtSigningKeyProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile SymmetricSecurityKey? _cachedKey;

    /// <summary>Creates the provider.</summary>
    public JwtSigningKeyProvider(
        IOptions<JwtOptions> options,
        ISecretClient secretClient,
        ILogger<JwtSigningKeyProvider> logger
    )
    {
        _options = options.Value;
        _secretClient = secretClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SigningCredentials> GetSigningCredentialsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var key = await GetKeyAsync(cancellationToken).ConfigureAwait(false);
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public async Task<SecurityKey> GetValidationKeyAsync(
        CancellationToken cancellationToken = default
    ) => await GetKeyAsync(cancellationToken).ConfigureAwait(false);

    private async Task<SymmetricSecurityKey> GetKeyAsync(CancellationToken cancellationToken)
    {
        // Fast path: already resolved.
        if (_cachedKey is not null)
        {
            return _cachedKey;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedKey is not null)
            {
                return _cachedKey;
            }

            var material = await ResolveKeyMaterialAsync(cancellationToken).ConfigureAwait(false);
            var bytes = Encoding.UTF8.GetBytes(material);
            if (bytes.Length < MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"The configured JWT signing key is too short ({bytes.Length} bytes). "
                        + $"A minimum of {MinimumKeyBytes} bytes (256 bits) is required for HMAC-SHA256."
                );
            }

            _logger.LogInformation(
                "JWT signing key resolved from {KeySource}.",
                _options.KeySource
            );
            _cachedKey = new SymmetricSecurityKey(bytes);
            return _cachedKey;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> ResolveKeyMaterialAsync(CancellationToken cancellationToken)
    {
        if (_options.KeySource == JwtKeySource.Inline)
        {
            if (string.IsNullOrWhiteSpace(_options.Key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key must be provided (via user-secrets, environment, or configuration) "
                        + "when Jwt:KeySource is 'Inline'. It must never be committed to source control."
                );
            }

            return _options.Key;
        }

        if (string.IsNullOrWhiteSpace(_options.KeySecretName))
        {
            throw new InvalidOperationException(
                $"Jwt:KeySecretName must be provided when Jwt:KeySource is '{_options.KeySource}'."
            );
        }

        var secret = await _secretClient
            .GetSecretAsync(_options.KeySecretName, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                $"The JWT signing key secret '{_options.KeySecretName}' could not be resolved "
                    + $"from source '{_options.KeySource}'."
            );
        }

        return secret;
    }
}
