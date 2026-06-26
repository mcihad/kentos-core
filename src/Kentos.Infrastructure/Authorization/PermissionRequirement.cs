using Microsoft.AspNetCore.Authorization;

namespace Kentos.Infrastructure.Authorization;

/// <summary>Authorization requirement carrying the permission key to check.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}
