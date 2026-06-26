using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Users;

public sealed record AssignUserRolesCommand(Guid UserId, IReadOnlyList<string> Roles) : ICommand<UserDetailResponse>;

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.Roles).NotNull().WithMessage("Rol listesi zorunludur.");
        RuleForEach(x => x.Roles).NotEmpty().WithMessage("Rol adı boş olamaz.");
    }
}

public static class AssignUserRolesHandler
{
    public static async Task<UserDetailResponse> Handle(
        AssignUserRolesCommand command, IUserService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.AssignRolesAsync(command, cancellationToken);
        await bus.PublishAsync(new UserRolesAssigned(result.Id, result.Roles));
        return result;
    }
}
