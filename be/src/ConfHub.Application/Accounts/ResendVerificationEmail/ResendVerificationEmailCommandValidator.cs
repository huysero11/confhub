using FluentValidation;

namespace ConfHub.Application.Accounts.ResendVerificationEmail;

public sealed class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();
    }
}
