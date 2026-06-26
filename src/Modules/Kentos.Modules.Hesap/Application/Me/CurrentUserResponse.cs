namespace Kentos.Modules.Hesap.Application.Me;

/// <summary>
/// The authenticated user's UI bootstrap context: identity, roles, and the effective
/// permission keys grouped by module slug — so the frontend can show/hide whole
/// modules and individual elements. Permissions are resolved from the token's roles
/// server-side (the token itself carries roles only).
/// </summary>
public sealed record CurrentUserResponse(
    string? UserId,
    string? UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Permissions);
