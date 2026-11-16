using FluentValidation;

namespace ConfHub.Application.Conferences.UpdateBreakSlot;

public sealed class UpdateBreakSlotCommandValidator : AbstractValidator<UpdateBreakSlotCommand>
{
    public UpdateBreakSlotCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 100)
            .WithErrorCode("NameLength")
            .WithMessage("Name must satisfy the required length (maximum 100).");

        RuleFor(command => command.StartTime).QuarterHour();

        RuleFor(command => command.EndTime).QuarterHour().EndAfterStart(command => command.StartTime);
    }
}
