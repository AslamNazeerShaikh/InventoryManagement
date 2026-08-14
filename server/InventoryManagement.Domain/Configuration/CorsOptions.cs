using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.Configuration;

/// <summary>
/// Cross-origin resource sharing configuration bound from the <c>Cors</c> section. Applied in all
/// environments (not just Development) so browser clients work in production.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cors";

    /// <summary>Named policy registered with the CORS middleware.</summary>
    public const string PolicyName = "DefaultCorsPolicy";

    /// <summary>Explicit list of allowed origins. Credentials require explicit origins (no wildcard).</summary>
    [MinLength(1)]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>Whether to allow credentials (cookies/authorization headers) on cross-origin requests.</summary>
    public bool AllowCredentials { get; set; } = true;
}
