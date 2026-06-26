namespace Kentos.Modules.Hesap.Authentication;

/// <summary>Login, refresh-token rotation, and logout.</summary>
public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
}
