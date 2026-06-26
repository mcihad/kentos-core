namespace Kentos.Modules.Hesap.Access;

/// <summary>Outcome of a login-time access policy evaluation.</summary>
public readonly record struct AccessDecision(bool Allowed, string? Reason)
{
    public static AccessDecision Allow() => new(true, null);
    public static AccessDecision Deny(string reason) => new(false, reason);
}

/// <summary>Evaluates a user's IP/time access policies (own + group) at login time.</summary>
public interface IAccessPolicyEvaluator
{
    Task<AccessDecision> EvaluateLoginAsync(long userId, string? ipAddress, CancellationToken cancellationToken);
}
