using FluentValidation;

namespace ConfHub.Application.Accounts.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.NewPassword).StrongPassword();
    }
}
