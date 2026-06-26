using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>
/// A refresh token. Only the SHA-256 hash of the opaque token is stored. Tokens are
/// rotated on use: the consumed token is revoked and linked to its replacement.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>SHA-256 hash (base64) of the opaque refresh token value.</summary>
    public string TokenHash { get; set; } = "";

    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    /// <summary>The token that superseded this one (set on rotation).</summary>
    public long? ReplacedById { get; set; }

    /// <summary>Client IP that requested the token.</summary>
    public string? Ip { get; set; }
}
