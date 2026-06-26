namespace Kentos.Modules.Hesap.Application.Roles;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    int PermissionCount,
    DateTimeOffset CreatedAt);

public sealed record RoleDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAt);
