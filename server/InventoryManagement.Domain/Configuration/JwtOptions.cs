using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.Configuration;

/// <summary>
/// Strongly-typed JWT configuration bound from the <c>Jwt</c> section. This is the single source
/// of truth used by both token <em>generation</em> (token service) and token <em>validation</em>
/// (JWT bearer middleware), eliminating the historical key/issuer/audience mismatch.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Describes where the signing key is sourced from. The literal <see cref="Key"/> is used only
    /// when <see cref="KeySource"/> is <c>Inline</c>; otherwise the key is resolved from a secret
    /// store (environment variable, mounted file, or cloud provider) at startup.
    /// </summary>
    public JwtKeySource KeySource { get; set; } = JwtKeySource.Inline;

    /// <summary>
    /// Inline symmetric signing key (used when <see cref="KeySource"/> is <c>Inline</c>).
    /// Should be supplied via user-secrets or environment configuration, never committed.
    /// Must be at least 32 bytes (256 bits) for HMAC-SHA256.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Name of the secret to resolve when <see cref="KeySource"/> is not <c>Inline</c>
    /// (e.g. environment variable name, file path, or cloud secret name/URI).
    /// </summary>
    public string? KeySecretName { get; set; }

    /// <summary>Token issuer. Must match between generation and validation.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token audience. Must match between generation and validation.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes.</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>Refresh-token lifetime in days.</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// Allowed clock skew (seconds) applied during validation. Defaults to 0 for strict expiry;
    /// small positive values tolerate minor clock drift between nodes.
    /// </summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 0;
}

/// <summary>Strategy describing where the JWT signing key is obtained from.</summary>
public enum JwtKeySource
{
    /// <summary>Read the literal key from configuration (appsettings / user-secrets / env override).</summary>
    Inline = 0,

    /// <summary>Read the key from an environment variable named by <see cref="JwtOptions.KeySecretName"/>.</summary>
    Environment = 1,

    /// <summary>Read the key from a file path (e.g. a Docker/Kubernetes mounted secret) named by <see cref="JwtOptions.KeySecretName"/>.</summary>
    File = 2,

    /// <summary>Resolve the key from a pluggable cloud secret provider (e.g. Azure Key Vault, AWS Secrets Manager).</summary>
    CloudSecret = 3,
}
