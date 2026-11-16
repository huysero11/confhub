using FluentValidation;

namespace ConfHub.Application.Conferences.UpdateConferenceDay;

public sealed class UpdateConferenceDayCommandValidator : AbstractValidator<UpdateConferenceDayCommand>
{
    public UpdateConferenceDayCommandValidator()
    {
        RuleFor(command => command.Date).NotEmpty();

        RuleFor(command => command.StartTime).QuarterHour();

        RuleFor(command => command.EndTime).ConferenceDayEnd(command => command.StartTime);
    }
}
