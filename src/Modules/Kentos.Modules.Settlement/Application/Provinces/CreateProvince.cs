using FluentValidation;
using Kentos.Modules.Settlement.Events;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Settlement.Application.Provinces;

public sealed record CreateProvinceCommand(string Name, int PlateCode) : ICommand<ProvinceResponse>;

public sealed class CreateProvinceCommandValidator : AbstractValidator<CreateProvinceCommand>
{
    public CreateProvinceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(128).WithMessage("Ad en fazla 128 karakter olabilir.");
        RuleFor(x => x.PlateCode).InclusiveBetween(1, 81).WithMessage("Plaka kodu 1 ile 81 arasında olmalıdır.");
    }
}

// Command handler: Wolverine validates (pipeline), the service does the work, then a
// domain event is published for decoupled consumers.
public static class CreateProvinceHandler
{
    public static async Task<ProvinceResponse> Handle(
        CreateProvinceCommand command, IProvinceService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new ProvinceCreated(result.Id, result.Name));
        return result;
    }
}
