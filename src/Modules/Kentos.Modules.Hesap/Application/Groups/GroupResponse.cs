namespace Kentos.Modules.Hesap.Application.Groups;

public sealed record GroupResponse(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    long Version,
    DateTimeOffset CreatedAt);

public sealed record GroupMemberResponse(Guid UserId, string UserName, string? DisplayName);
