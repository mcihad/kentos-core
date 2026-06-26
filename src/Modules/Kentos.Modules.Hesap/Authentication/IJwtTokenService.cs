using Kentos.Modules.Hesap.Domain;

namespace Kentos.Modules.Hesap.Authentication;

/// <summary>Issues self-signed access tokens (roles only) and opaque refresh tokens.</summary>
public interface IJwtTokenService
{
    /// <summary>Builds a signed access token carrying the user's identity and role claims.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>Generates a fresh opaque refresh token together with its hash and expiry.</summary>
    (string Raw, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    /// <summary>Computes the lookup hash for a presented refresh token value.</summary>
    string HashRefreshToken(string raw);
}
