using FluentValidation;
using Kentos.Modules.Hesap.Events;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Cqrs;
using Wolverine;

namespace Kentos.Modules.Hesap.Application.Users;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string? DisplayName,
    string Password,
    IReadOnlyList<string>? Roles) : ICommand<UserResponse>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(256).WithMessage("Kullanıcı adı en fazla 256 karakter olabilir.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır.");
    }
}

public static class CreateUserHandler
{
    public static async Task<UserResponse> Handle(
        CreateUserCommand command, IUserService service, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(command, cancellationToken);
        await bus.PublishAsync(new UserCreated(result.Id, result.UserName));
        return result;
    }
}
