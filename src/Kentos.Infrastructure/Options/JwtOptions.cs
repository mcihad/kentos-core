namespace Kentos.Infrastructure.Options;

/// <summary>
/// Settings for the self-issued JWTs (the "Jwt" section). The application both issues
/// (Hesap module login) and validates (this resource server) its own tokens using a
/// symmetric signing key. Access tokens carry roles only.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (iss).</summary>
    public string Issuer { get; set; } = "kentos";

    /// <summary>Expected audience (aud).</summary>
    public string Audience { get; set; } = "kentos";

    /// <summary>Symmetric HMAC-SHA256 signing key. Must be supplied via configuration/secret.</summary>
    public string SigningKey { get; set; } = "";

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenDays { get; set; } = 14;
}
