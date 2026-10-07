using System.Text.Json;
using ConfHub.Api.ErrorHandling;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Domain.Common;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConfHub.Api.IntegrationTests.ErrorHandling;

public class ExceptionHandlingMiddlewareTests
{
    public static TheoryData<Exception, int, string> Mappings => new()
    {
        { new NotFoundException("Conference", 1), StatusCodes.Status404NotFound, "NotFound" },
        { new ConflictException("User.EmailTaken", "Email taken"), StatusCodes.Status409Conflict, "User.EmailTaken" },
        { new DomainException("Session.SpeakerBusy", "Speaker busy"), StatusCodes.Status422UnprocessableEntity, "Session.SpeakerBusy" },
        { new UnauthorizedException("SessionExpired", "Expired"), StatusCodes.Status401Unauthorized, "SessionExpired" },
        { new ForbiddenException("No access"), StatusCodes.Status403Forbidden, "Forbidden" },
        { new ForbiddenException("AccountLocked", "Locked"), StatusCodes.Status403Forbidden, "AccountLocked" },
        { new InvalidOperationException("Boom"), StatusCodes.Status500InternalServerError, "Internal" },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task ExceptionIsMappedToStatusAndCode(Exception exception, int expectedStatus, string expectedCode)
    {
        var context = await RunMiddlewareAsync(exception, Environments.Development);
        var body = ReadBody(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(expectedCode, body.GetProperty("code").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task ValidationExceptionReturnsErrorCodesPerField()
    {
        var failure = new ValidationFailure("Title", "Title must not be empty.") { ErrorCode = "NotEmptyValidator" };

        var context = await RunMiddlewareAsync(new ValidationException([failure]), Environments.Development);
        var body = ReadBody(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Validation", body.GetProperty("code").GetString());
        var titleErrors = body.GetProperty("errors").GetProperty("Title");
        Assert.Equal("NotEmptyValidator", Assert.Single(titleErrors.EnumerateArray()).GetString());
    }

    [Fact]
    public async Task InternalErrorHidesMessageOutsideDevelopment()
    {
        var context = await RunMiddlewareAsync(new InvalidOperationException("Secret table name"), Environments.Production);
        var body = ReadBody(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.False(body.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task InternalErrorShowsMessageInDevelopment()
    {
        var context = await RunMiddlewareAsync(new InvalidOperationException("Boom"), Environments.Development);
        var body = ReadBody(context);

        Assert.Equal("Boom", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ClientAbortedRequestWritesNothing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var context = await RunMiddlewareAsync(new OperationCanceledException(), Environments.Development, cancellation.Token);

        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task RequestWithoutErrorPassesThrough()
    {
        var middleware = CreateMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            Environments.Development);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next, string environmentName)
    {
        return new ExceptionHandlingMiddleware(
            next,
            new HostingEnvironment { EnvironmentName = environmentName },
            NullLogger<ExceptionHandlingMiddleware>.Instance);
    }

    // Chạy middleware với một "phần phía sau" giả luôn ném exception cho trước.
    private static async Task<HttpContext> RunMiddlewareAsync(
        Exception exception,
        string environmentName,
        CancellationToken requestAborted = default)
    {
        var middleware = CreateMiddleware(_ => throw exception, environmentName);
        var context = new DefaultHttpContext { RequestAborted = requestAborted };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        return context;
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = JsonDocument.Parse(context.Response.Body);
        return document.RootElement.Clone();
    }
}
