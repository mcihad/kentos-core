using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Groups;

public sealed record UpdateGroupCommand(Guid Id, string Name, string? Description) : ICommand<GroupResponse>;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Grup adı zorunludur.")
            .MaximumLength(256).WithMessage("Grup adı en fazla 256 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(512).WithMessage("Açıklama en fazla 512 karakter olabilir.");
    }
}

public static class UpdateGroupHandler
{
    public static Task<GroupResponse> Handle(
        UpdateGroupCommand command, IUserGroupService service, CancellationToken cancellationToken) =>
        service.UpdateAsync(command, cancellationToken);
}
