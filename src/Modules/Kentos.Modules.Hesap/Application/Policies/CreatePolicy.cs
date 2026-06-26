using FluentValidation;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Policies;

public sealed record CreatePolicyCommand(
    PolicySubjectType SubjectType,
    Guid SubjectId,
    PolicyKind Kind,
    PolicyEffect Effect,
    string Value,
    int Priority) : ICommand<PolicyResponse>;

public sealed class CreatePolicyCommandValidator : AbstractValidator<CreatePolicyCommand>
{
    public CreatePolicyCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Konu kimliği zorunludur.");
        RuleFor(x => x.Value).NotEmpty().WithMessage("Değer zorunludur.")
            .MaximumLength(128).WithMessage("Değer en fazla 128 karakter olabilir.");
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0).WithMessage("Öncelik 0 veya daha büyük olmalıdır.");
    }
}

public static class CreatePolicyHandler
{
    public static async Task<PolicyResponse> Handle(
        CreatePolicyCommand command, IAccessPolicyService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new AccessPolicyCreated(result.Id));
        return result;
    }
}
