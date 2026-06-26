using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Groups;

public sealed record AddGroupMemberCommand(Guid GroupId, Guid UserId) : ICommand;

public sealed class AddGroupMemberCommandValidator : AbstractValidator<AddGroupMemberCommand>
{
    public AddGroupMemberCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı kimliği zorunludur.");
    }
}

public static class AddGroupMemberHandler
{
    public static Task Handle(
        AddGroupMemberCommand command, IUserGroupService service, CancellationToken cancellationToken) =>
        service.AddMemberAsync(command, cancellationToken);
}
