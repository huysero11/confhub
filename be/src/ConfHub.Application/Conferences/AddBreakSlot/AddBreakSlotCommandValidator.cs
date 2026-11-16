using FluentValidation;

namespace ConfHub.Application.Conferences.AddBreakSlot;

public sealed class AddBreakSlotCommandValidator : AbstractValidator<AddBreakSlotCommand>
{
    public AddBreakSlotCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 100)
            .WithErrorCode("NameLength")
            .WithMessage("Name must satisfy the required length (maximum 100).");

        RuleFor(command => command.StartTime).QuarterHour();

        RuleFor(command => command.EndTime).QuarterHour().EndAfterStart(command => command.StartTime);
    }
}
