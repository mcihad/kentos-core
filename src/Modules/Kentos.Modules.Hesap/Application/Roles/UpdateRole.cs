using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Roles;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : ICommand<RoleResponse>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(128).WithMessage("Rol adı en fazla 128 karakter olabilir.");
        RuleFor(x => x.Description).MaximumLength(512).WithMessage("Açıklama en fazla 512 karakter olabilir.");
    }
}

public static class UpdateRoleHandler
{
    public static Task<RoleResponse> Handle(
        UpdateRoleCommand command, IRoleService service, CancellationToken cancellationToken) =>
        service.UpdateAsync(command, cancellationToken);
}
