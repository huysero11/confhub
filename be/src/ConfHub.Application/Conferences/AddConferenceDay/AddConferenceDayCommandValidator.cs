using FluentValidation;

namespace ConfHub.Application.Conferences.AddConferenceDay;

public sealed class AddConferenceDayCommandValidator : AbstractValidator<AddConferenceDayCommand>
{
    public AddConferenceDayCommandValidator()
    {
        RuleFor(command => command.Date).NotEmpty();

        RuleFor(command => command.StartTime).QuarterHour();

        RuleFor(command => command.EndTime).ConferenceDayEnd(command => command.StartTime);
    }
}
