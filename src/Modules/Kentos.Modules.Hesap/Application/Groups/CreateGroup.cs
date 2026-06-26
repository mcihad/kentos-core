using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Groups;

public sealed record CreateGroupCommand(string Name, string? Description) : ICommand<GroupResponse>;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Grup adı zorunludur.")
            .MaximumLength(256).WithMessage("Grup adı en fazla 256 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(512).WithMessage("Açıklama en fazla 512 karakter olabilir.");
    }
}

public static class CreateGroupHandler
{
    public static async Task<GroupResponse> Handle(
        CreateGroupCommand command, IUserGroupService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new UserGroupCreated(result.Id, result.Name));
        return result;
    }
}
