using Kentos.Modules.Hesap.Access;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Authentication;

/// <summary>Orchestrates credential checks, login-time policy enforcement and token issuance.</summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly HesapDbContext _db;
    private readonly IJwtTokenService _tokens;
    private readonly IAccessPolicyEvaluator _policies;
    private readonly TimeProvider _clock;

    public AuthService(
        UserManager<ApplicationUser> users,
        HesapDbContext db,
        IJwtTokenService tokens,
        IAccessPolicyEvaluator policies,
        TimeProvider clock)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
        _policies = policies;
        _clock = clock;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await _users.FindByNameAsync(request.UserName);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Kullanıcı adı veya parola hatalı.");
        }

        var decision = await _policies.EvaluateLoginAsync(user.Id, ipAddress, cancellationToken);
        if (!decision.Allowed)
        {
            throw new ForbiddenException(decision.Reason ?? "Erişim politikası nedeniyle giriş reddedildi.");
        }

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || existing.IsRevoked || existing.ExpiresAt <= _clock.GetUtcNow())
        {
            throw new UnauthorizedException("Yenileme tokenı geçersiz veya süresi dolmuş.");
        }

        var user = await _users.FindByIdAsync(existing.UserId.ToString())
            ?? throw new UnauthorizedException("Kullanıcı bulunamadı.");

        existing.IsRevoked = true;
        var response = await IssueTokensAsync(user, ipAddress, cancellationToken, existing);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is { IsRevoked: false })
        {
            existing.IsRevoked = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<TokenResponse> IssueTokensAsync(
        ApplicationUser user, string? ipAddress, CancellationToken cancellationToken, RefreshToken? rotatedFrom = null)
    {
        var roles = await _users.GetRolesAsync(user);
        var (accessToken, accessExpires) = _tokens.CreateAccessToken(user, roles);
        var (rawRefresh, refreshHash, refreshExpires) = _tokens.CreateRefreshToken();

        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpires,
            Ip = ipAddress,
        };

        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(cancellationToken);

        if (rotatedFrom is not null)
        {
            rotatedFrom.ReplacedById = refresh.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new TokenResponse(accessToken, accessExpires, rawRefresh, refreshExpires);
    }
}
