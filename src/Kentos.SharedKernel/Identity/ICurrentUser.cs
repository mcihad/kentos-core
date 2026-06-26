namespace Kentos.SharedKernel.Identity;

/// <summary>Access to the current request's user (for audit, errors, authorization).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool HasPermission(string permission);
}
