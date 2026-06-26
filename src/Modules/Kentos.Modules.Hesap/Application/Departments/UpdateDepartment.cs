using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Departments;

public sealed record UpdateDepartmentCommand(Guid Id, string Name, Guid? ParentId) : ICommand<DepartmentResponse>;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Departman adı zorunludur.")
            .MaximumLength(256).WithMessage("Departman adı en fazla 256 karakter olabilir.");
    }
}

public static class UpdateDepartmentHandler
{
    public static Task<DepartmentResponse> Handle(
        UpdateDepartmentCommand command, IDepartmentService service, CancellationToken cancellationToken) =>
        service.UpdateAsync(command, cancellationToken);
}
