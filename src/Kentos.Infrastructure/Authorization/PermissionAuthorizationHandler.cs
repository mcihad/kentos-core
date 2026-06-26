using Kentos.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Kentos.Infrastructure.Authorization;

/// <summary>
/// Succeeds when one of the JWT's "roles" grants the required permission.
/// The token carries roles only; the role → permission map is resolved server-side
/// via <see cref="IPermissionResolver"/> (cached), so tokens stay small no matter how
/// many permissions exist.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionResolver _resolver;

    public PermissionAuthorizationHandler(IPermissionResolver resolver) => _resolver = resolver;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var roles = context.User.FindAll("roles").Select(c => c.Value);
            if (_resolver.ResolvePermissions(roles).Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
