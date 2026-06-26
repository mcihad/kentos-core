using Microsoft.AspNetCore.Authorization;

namespace Kentos.Infrastructure.Authorization;

/// <summary>Succeeds when the JWT carries the required permission in its "permissions" claims.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User.Identity?.IsAuthenticated == true
            && context.User.FindAll("permissions")
                .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
