using ConfHub.Domain.Accounts;
using FluentValidation;

namespace ConfHub.Application.Accounts.Register;

// BR01–BR05. Mã lỗi (ErrorCode) được trả về từng trường để FE dịch sang vi/en.
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.FullName)
            .Must(fullName => fullName is not null && fullName.Trim().Length is >= 2 and <= 100)
            .WithErrorCode("FullNameLength")
            .WithMessage("Full name must be between 2 and 100 characters.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(command => command.Password).StrongPassword();

        RuleFor(command => command.Role)
            .Must(IsSelfRegisterRole)
            .WithErrorCode(AccountErrorCodes.RoleNotAllowed)
            .WithMessage("Only Attendee, Organizer or Supplier can sign up.");

        RuleFor(command => command.Organization)
            .MaximumLength(200);

        RuleFor(command => command.Organization)
            .NotEmpty()
            .When(command => command.Role is RoleCodes.Organizer or RoleCodes.Supplier)
            .WithErrorCode(AccountErrorCodes.OrganizationRequired)
            .WithMessage("Organization is required for organizers and suppliers.");
    }

    private static bool IsSelfRegisterRole(string? role)
    {
        return role is RoleCodes.Attendee or RoleCodes.Organizer or RoleCodes.Supplier;
    }
}
