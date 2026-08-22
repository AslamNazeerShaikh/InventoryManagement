using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace InventoryManagement.API.Infrastructure.Authorization;

/// <summary>
/// Dynamic <see cref="IAuthorizationPolicyProvider"/> that materializes a permission policy on demand
/// for any policy name using the <see cref="HasPermissionAttribute.PolicyPrefix"/>, so controllers
/// can require arbitrary (including tenant-defined) permission codes without registering each one.
/// Non-permission policy names fall through to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    /// <summary>Creates the provider over the framework default provider.</summary>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
