using FluentValidation;

namespace ConfHub.Application.Tracks.CreateTrack;

public sealed class CreateTrackCommandValidator : AbstractValidator<CreateTrackCommand>
{
    public CreateTrackCommandValidator()
    {
        RuleFor(command => command.NameVi)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 100)
            .WithErrorCode("NameViLength")
            .WithMessage("NameVi must satisfy the required length (maximum 100).");

        RuleFor(command => command.NameEn)
            .Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 100)
            .WithErrorCode("NameEnLength")
            .WithMessage("NameEn must satisfy the required length (maximum 100).");
    }
}
