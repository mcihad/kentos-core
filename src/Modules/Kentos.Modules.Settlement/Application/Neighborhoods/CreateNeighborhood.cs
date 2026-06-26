using FluentValidation;
using Kentos.Modules.Settlement.Events;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Settlement.Application.Neighborhoods;

public sealed record CreateNeighborhoodCommand(
    string Name,
    Guid DistrictId,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? BoundaryWkt) : ICommand<NeighborhoodResponse>;

public sealed class CreateNeighborhoodCommandValidator : AbstractValidator<CreateNeighborhoodCommand>
{
    public CreateNeighborhoodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(128).WithMessage("Ad en fazla 128 karakter olabilir.");
        RuleFor(x => x.DistrictId).NotEmpty().WithMessage("İlçe seçilmelidir.");
        RuleFor(x => x.PostalCode).MaximumLength(16).WithMessage("Posta kodu en fazla 16 karakter olabilir.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("Enlem -90 ile 90 arasında olmalıdır.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("Boylam -180 ile 180 arasında olmalıdır.");
    }
}

public static class CreateNeighborhoodHandler
{
    public static async Task<NeighborhoodResponse> Handle(
        CreateNeighborhoodCommand command, INeighborhoodService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new NeighborhoodCreated(result.Id, result.Name, result.DistrictId));
        return result;
    }
}
