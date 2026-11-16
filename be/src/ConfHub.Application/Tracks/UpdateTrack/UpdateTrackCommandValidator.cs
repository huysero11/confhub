using FluentValidation;

namespace ConfHub.Application.Tracks.UpdateTrack;

public sealed class UpdateTrackCommandValidator : AbstractValidator<UpdateTrackCommand>
{
    public UpdateTrackCommandValidator()
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
