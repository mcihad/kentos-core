using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Users;

public sealed record SetUserDepartmentsCommand(Guid UserId, IReadOnlyList<Guid> DepartmentIds)
    : ICommand<UserDetailResponse>;

public sealed class SetUserDepartmentsCommandValidator : AbstractValidator<SetUserDepartmentsCommand>
{
    public SetUserDepartmentsCommandValidator()
    {
        RuleFor(x => x.DepartmentIds).NotNull().WithMessage("Departman listesi zorunludur.");
    }
}

public static class SetUserDepartmentsHandler
{
    public static Task<UserDetailResponse> Handle(
        SetUserDepartmentsCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.SetDepartmentsAsync(command, cancellationToken);
}
