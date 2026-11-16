using FluentValidation;

namespace ConfHub.Application.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 50)
            .WithErrorCode("NameLength")
            .WithMessage("Name must satisfy the required length (maximum 50).");

        RuleFor(command => command.Capacity)
            .InclusiveBetween(1, 10000)
            .WithErrorCode("CapacityRange")
            .WithMessage("Capacity must be between 1 and 10000.");
    }
}
