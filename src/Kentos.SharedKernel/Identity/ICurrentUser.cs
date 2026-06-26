namespace Kentos.SharedKernel.Identity;

/// <summary>Access to the current request's user (for audit, errors, authorization).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }

    /// <summary>Role names carried directly by the JWT.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Permissions resolved from <see cref="Roles"/> via the role → permission map.</summary>
    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
