using InventoryManagement.Domain.Security;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Security;

/// <summary>
/// Default <see cref="ISecretClient"/> for local/self-hosted deployments. Resolves a secret name to
/// either the contents of an existing file (e.g. a Docker/Kubernetes mounted secret) or, failing
/// that, an environment variable of the same name. Replace this registration with a cloud-backed
/// implementation (e.g. Azure Key Vault, AWS Secrets Manager) to source secrets from the cloud.
/// </summary>
public sealed class EnvironmentFileSecretClient : ISecretClient
{
    private readonly ILogger<EnvironmentFileSecretClient> _logger;

    /// <summary>Creates the client.</summary>
    public EnvironmentFileSecretClient(ILogger<EnvironmentFileSecretClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (File.Exists(secretName))
        {
            _logger.LogInformation("Resolving secret {SecretName} from mounted file.", secretName);
            var fileValue = await File.ReadAllTextAsync(secretName, cancellationToken)
                .ConfigureAwait(false);
            return fileValue.Trim();
        }

        var envValue = Environment.GetEnvironmentVariable(secretName);
        if (!string.IsNullOrEmpty(envValue))
        {
            _logger.LogInformation(
                "Resolving secret {SecretName} from environment variable.",
                secretName
            );
            return envValue;
        }

        _logger.LogWarning(
            "Secret {SecretName} could not be resolved from file or environment.",
            secretName
        );
        return null;
    }
}
