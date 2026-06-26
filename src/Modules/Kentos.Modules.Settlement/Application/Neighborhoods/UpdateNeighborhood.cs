using FluentValidation;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Settlement.Application.Neighborhoods;

public sealed record UpdateNeighborhoodCommand(
    Guid Id,
    string Name,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? BoundaryWkt) : ICommand<NeighborhoodResponse>;

public sealed class UpdateNeighborhoodCommandValidator : AbstractValidator<UpdateNeighborhoodCommand>
{
    public UpdateNeighborhoodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Kimlik zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(128).WithMessage("Ad en fazla 128 karakter olabilir.");
        RuleFor(x => x.PostalCode).MaximumLength(16).WithMessage("Posta kodu en fazla 16 karakter olabilir.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue)
            .WithMessage("Enlem -90 ile 90 arasında olmalıdır.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue)
            .WithMessage("Boylam -180 ile 180 arasında olmalıdır.");
    }
}

public static class UpdateNeighborhoodHandler
{
    public static Task<NeighborhoodResponse> Handle(UpdateNeighborhoodCommand command, INeighborhoodService service, CancellationToken cancellationToken)
        => service.UpdateAsync(command, cancellationToken);
}
