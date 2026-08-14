namespace InventoryManagement.Domain.Security;

/// <summary>
/// Resolves named secrets from a backing store. Implementations provide local (environment/file)
/// and cloud (e.g. Azure Key Vault, AWS Secrets Manager) sources, selected by configuration, so
/// the same code path serves local development and production deployments.
/// </summary>
public interface ISecretClient
{
    /// <summary>
    /// Retrieves the secret value for the given logical name, or <c>null</c> if not found.
    /// </summary>
    /// <param name="secretName">Provider-specific secret identifier (env var, file path, or cloud name/URI).</param>
    /// <param name="cancellationToken">Token used to cancel the (potentially remote) lookup.</param>
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
}
