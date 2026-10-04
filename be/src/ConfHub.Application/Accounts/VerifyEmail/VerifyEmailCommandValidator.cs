using FluentValidation;

namespace ConfHub.Application.Accounts.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .MaximumLength(100);
    }
}
