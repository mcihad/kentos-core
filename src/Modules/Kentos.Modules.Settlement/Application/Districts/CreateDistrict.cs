using FluentValidation;
using Kentos.Modules.Settlement.Events;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Settlement.Application.Districts;

public sealed record CreateDistrictCommand(string Name, Guid ProvinceId) : ICommand<DistrictResponse>;

public sealed class CreateDistrictCommandValidator : AbstractValidator<CreateDistrictCommand>
{
    public CreateDistrictCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(128).WithMessage("Ad en fazla 128 karakter olabilir.");
        RuleFor(x => x.ProvinceId).NotEmpty().WithMessage("İl seçilmelidir.");
    }
}

public static class CreateDistrictHandler
{
    public static async Task<DistrictResponse> Handle(
        CreateDistrictCommand command, IDistrictService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new DistrictCreated(result.Id, result.Name, result.ProvinceId));
        return result;
    }
}
