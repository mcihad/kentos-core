namespace Kentos.Modules.Hesap.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string UserName,
    string? Email,
    string? DisplayName,
    bool LockoutEnabled,
    DateTimeOffset CreatedAt);

public sealed record UserDetailResponse(
    Guid Id,
    string UserName,
    string? Email,
    string? DisplayName,
    bool LockoutEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> Departments,
    DateTimeOffset CreatedAt);
