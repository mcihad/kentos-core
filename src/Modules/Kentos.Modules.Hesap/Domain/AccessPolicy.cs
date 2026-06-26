using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Whom a policy targets.</summary>
public enum PolicySubjectType
{
    User = 0,
    Group = 1,
}

/// <summary>What a policy constrains.</summary>
public enum PolicyKind
{
    /// <summary>Time-of-day window, value formatted "HH:mm-HH:mm".</summary>
    Time = 0,

    /// <summary>IP address or CIDR range, value formatted as a CIDR (e.g. "10.0.0.0/8").</summary>
    Ip = 1,
}

/// <summary>Whether the policy allows or denies the matched condition.</summary>
public enum PolicyEffect
{
    Allow = 0,
    Deny = 1,
}

/// <summary>
/// A login-time access policy. Policies are evaluated only at login: a user's matched
/// deny policies (own or via group membership) block the login; otherwise it proceeds.
/// </summary>
public sealed class AccessPolicy : BaseEntity
{
    public PolicySubjectType SubjectType { get; set; }

    /// <summary>Internal id of the targeted user or group (per <see cref="SubjectType"/>).</summary>
    public long SubjectId { get; set; }

    public PolicyKind Kind { get; set; }
    public PolicyEffect Effect { get; set; }

    /// <summary>CIDR (for <see cref="PolicyKind.Ip"/>) or "HH:mm-HH:mm" (for <see cref="PolicyKind.Time"/>).</summary>
    public string Value { get; set; } = "";

    /// <summary>Evaluation order; lower runs first. Deny wins on equal priority.</summary>
    public int Priority { get; set; }
}
