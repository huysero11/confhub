using FluentValidation;

namespace ConfHub.Application.Tests.Common.Behaviors;

public sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
{
    public SampleRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty();
    }
}
