using Kentos.Modules.Hesap.Domain;

namespace Kentos.Modules.Hesap.Application.Policies;

public sealed record PolicyResponse(
    Guid Id,
    PolicySubjectType SubjectType,
    Guid SubjectId,
    PolicyKind Kind,
    PolicyEffect Effect,
    string Value,
    int Priority);
