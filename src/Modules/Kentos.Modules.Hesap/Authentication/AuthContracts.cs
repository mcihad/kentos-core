namespace Kentos.Modules.Hesap.Authentication;

/// <summary>Username + password login request.</summary>
public sealed record LoginRequest(string UserName, string Password);

/// <summary>Refresh request carrying a previously issued refresh token.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Logout request revoking a refresh token.</summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>Issued token pair.</summary>
public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
