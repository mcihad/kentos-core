using FluentValidation;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;

namespace Kentos.Modules.Hesap.Application.Users;

public sealed record UpdateUserCommand(Guid Id, string? Email, string? DisplayName, bool LockoutEnabled)
    : ICommand<UserResponse>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.DisplayName).MaximumLength(256).WithMessage("Ad soyad en fazla 256 karakter olabilir.");
    }
}

public static class UpdateUserHandler
{
    public static Task<UserResponse> Handle(
        UpdateUserCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.UpdateAsync(command, cancellationToken);
}
