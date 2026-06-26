using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kentos.Infrastructure.Options;
using Kentos.Modules.Hesap.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kentos.Modules.Hesap.Authentication;

/// <summary>Symmetric (HMAC-SHA256) JWT issuer. Access tokens carry roles only.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Uuid.ToString()),
            new("preferred_username", user.UserName ?? user.Uuid.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Roles only — never permissions (keeps the token small).
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string Raw, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expires = _clock.GetUtcNow().AddDays(_options.RefreshTokenDays);
        return (raw, HashRefreshToken(raw), expires);
    }

    public string HashRefreshToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }
}
