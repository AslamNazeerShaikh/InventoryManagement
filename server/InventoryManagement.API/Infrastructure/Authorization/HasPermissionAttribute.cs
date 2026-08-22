using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.API.Infrastructure.Authorization;

/// <summary>
/// Requires the caller to hold a specific permission. Sugar over an authorization policy whose name
/// is the permission prefixed with <see cref="PolicyPrefix"/>; the <see cref="PermissionPolicyProvider"/>
/// materializes the policy on demand, so any permission code can be required without pre-registration.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true
)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Policy-name prefix identifying a permission policy.</summary>
    public const string PolicyPrefix = "perm:";

    /// <summary>Requires the given permission code.</summary>
    public HasPermissionAttribute(string permission) => Policy = $"{PolicyPrefix}{permission}";
}
