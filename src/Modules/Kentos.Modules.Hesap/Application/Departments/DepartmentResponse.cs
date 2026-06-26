namespace Kentos.Modules.Hesap.Application.Departments;

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    long Version,
    DateTimeOffset CreatedAt);
