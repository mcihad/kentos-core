using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Roles;

public sealed record AssignRolePermissionsCommand(Guid RoleId, IReadOnlyList<string> PermissionKeys)
    : ICommand<RoleDetailResponse>;

public sealed class AssignRolePermissionsCommandValidator : AbstractValidator<AssignRolePermissionsCommand>
{
    public AssignRolePermissionsCommandValidator()
    {
        RuleFor(x => x.PermissionKeys).NotNull().WithMessage("Yetki listesi zorunludur.");
        RuleForEach(x => x.PermissionKeys).NotEmpty().WithMessage("Yetki anahtarı boş olamaz.");
    }
}

public static class AssignRolePermissionsHandler
{
    public static async Task<RoleDetailResponse> Handle(
        AssignRolePermissionsCommand command, IRoleService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.AssignPermissionsAsync(command, cancellationToken);
        await bus.PublishAsync(new RolePermissionsAssigned(result.Id, result.Permissions.Count));
        return result;
    }
}
