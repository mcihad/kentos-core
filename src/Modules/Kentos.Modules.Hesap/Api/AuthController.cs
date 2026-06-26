using Asp.Versioning;
using Kentos.Infrastructure.DependencyInjection;
using Kentos.Modules.Hesap.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kentos.Modules.Hesap.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hesap/auth")]
[AllowAnonymous]
[EnableRateLimiting(ApiExtensions.AuthRateLimitPolicy)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [EndpointSummary("Giriş yap")]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken ct) =>
        _auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    [HttpPost("refresh")]
    [EndpointSummary("Tokenı yenile")]
    public Task<TokenResponse> Refresh(RefreshRequest request, CancellationToken ct) =>
        _auth.RefreshAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    [HttpPost("logout")]
    [EndpointSummary("Çıkış yap")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }
}
