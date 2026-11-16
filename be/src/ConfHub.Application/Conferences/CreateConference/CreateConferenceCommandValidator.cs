using ConfHub.Domain.Conferences;
using FluentValidation;

namespace ConfHub.Application.Conferences.CreateConference;

public sealed class CreateConferenceCommandValidator : AbstractValidator<CreateConferenceCommand>
{
    public CreateConferenceCommandValidator()
    {
        RuleFor(command => command.NameVi)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 200)
            .WithErrorCode("NameViLength")
            .WithMessage("NameVi must satisfy the required length (maximum 200).");

        RuleFor(command => command.NameEn)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 200)
            .WithErrorCode("NameEnLength")
            .WithMessage("NameEn must satisfy the required length (maximum 200).");

        RuleFor(command => command.DescriptionVi)
            .TranslationOf(command => command.DescriptionEn)
            .Must(text => text is null || text.Trim().Length <= 4000)
            .WithErrorCode("DescriptionViLength")
            .WithMessage("DescriptionVi must satisfy the required length (maximum 4000).");

        RuleFor(command => command.DescriptionEn)
            .TranslationOf(command => command.DescriptionVi)
            .Must(text => text is null || text.Trim().Length <= 4000)
            .WithErrorCode("DescriptionEnLength")
            .WithMessage("DescriptionEn must satisfy the required length (maximum 4000).");

        RuleFor(command => command.Venue)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 300)
            .WithErrorCode("VenueLength")
            .WithMessage("Venue must satisfy the required length (maximum 300).");

        RuleFor(command => command.RegistrationCloseAt).NotEmpty();

        RuleFor(command => command.StartDate).NotEmpty();

        RuleFor(command => command.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithErrorCode("EndDateBeforeStart")
            .WithMessage("EndDate must be on or after StartDate.");

        RuleFor(command => command.EndDate)
            .Must((command, endDate) => endDate.DayNumber - command.StartDate.DayNumber + 1 <= Conference.MaxDays)
            .WithErrorCode(ConferenceErrorCodes.TooManyDays);

        RuleFor(command => command.DayStartTime).QuarterHour();

        RuleFor(command => command.DayEndTime).ConferenceDayEnd(command => command.DayStartTime);
    }
}
