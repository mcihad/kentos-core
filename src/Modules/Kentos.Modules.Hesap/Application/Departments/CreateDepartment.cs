using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Departments;

public sealed record CreateDepartmentCommand(string Name, Guid? ParentId) : ICommand<DepartmentResponse>;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(256).WithMessage("Departman adı en fazla 256 karakter olabilir.");
    }
}

public static class CreateDepartmentHandler
{
    public static async Task<DepartmentResponse> Handle(
        CreateDepartmentCommand command, IDepartmentService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new DepartmentCreated(result.Id, result.Name));
        return result;
    }
}
