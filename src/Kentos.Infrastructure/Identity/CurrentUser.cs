using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Kentos.Infrastructure.Identity;

/// <summary>Scoped service resolving the current user from HttpContext claims.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IPermissionResolver _resolver;
    private string[]? _roles;
    private string[]? _permissions;

    public CurrentUser(IHttpContextAccessor accessor, IPermissionResolver resolver)
    {
        _accessor = accessor;
        _resolver = resolver;
    }

    private HttpContext? Context => _accessor.HttpContext;

    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated ?? false;

    public string? UserId => Context?.User.FindFirst("sub")?.Value;

    public string? UserName =>
        Context?.User.FindFirst("preferred_username")?.Value ?? Context?.User.Identity?.Name;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public IReadOnlyCollection<string> Roles => _roles ??= LoadRoles();

    public IReadOnlyCollection<string> Permissions =>
        _permissions ??= _resolver.ResolvePermissions(Roles).ToArray();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.Ordinal);

    private string[] LoadRoles() =>
        Context?.User.FindAll("roles").Select(c => c.Value).ToArray() ?? [];
}
