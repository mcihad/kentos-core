using Kentos.SharedKernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Kentos.Infrastructure.Identity;

/// <summary>Scoped service resolving the current user from HttpContext claims.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    private string[]? _permissions;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private HttpContext? Context => _accessor.HttpContext;

    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated ?? false;

    public string? UserId => Context?.User.FindFirst("sub")?.Value;

    public string? UserName =>
        Context?.User.FindFirst("preferred_username")?.Value ?? Context?.User.Identity?.Name;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public IReadOnlyCollection<string> Permissions => _permissions ??= LoadPermissions();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.Ordinal);

    private string[] LoadPermissions() =>
        Context?.User.FindAll("permissions").Select(c => c.Value).ToArray() ?? [];
}
