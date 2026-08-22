using InventoryManagement.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.API.Infrastructure.Authorization;

/// <summary>
/// Grants a <see cref="PermissionRequirement"/> when the caller's token carries a matching
/// <c>permission</c> claim. Authorization is fully stateless (claims-based), so no database access
/// occurs on the request path — permissions are resolved once at token-issuance time.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        if (context.User.HasClaim(AuthConstants.Claims.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
