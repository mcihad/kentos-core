using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Roles;

public sealed record CreateRoleCommand(string Name, string? Description) : ICommand<RoleResponse>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(128).WithMessage("Rol adı en fazla 128 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(512).WithMessage("Açıklama en fazla 512 karakter olabilir.");
    }
}

public static class CreateRoleHandler
{
    public static async Task<RoleResponse> Handle(
        CreateRoleCommand command, IRoleService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new RoleCreated(result.Id, result.Name));
        return result;
    }
}
