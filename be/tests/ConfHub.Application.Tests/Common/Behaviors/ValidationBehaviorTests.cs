using ConfHub.Application.Common.Behaviors;
using FluentValidation;

namespace ConfHub.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task ValidRequestCallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);

        var response = await behavior.Handle(
            new SampleRequest("Công nghệ Xanh 2026"),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", response);
    }

    [Fact]
    public async Task InvalidRequestThrowsAndDoesNotCallNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleRequestValidator()]);
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new SampleRequest(string.Empty),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            CancellationToken.None));

        var failure = Assert.Single(exception.Errors);
        Assert.Equal(nameof(SampleRequest.Title), failure.PropertyName);
        Assert.Equal("NotEmptyValidator", failure.ErrorCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task RequestWithoutValidatorCallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([]);

        var response = await behavior.Handle(
            new SampleRequest(string.Empty),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", response);
    }
}
