using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ConfHub.Api.RateLimiting;

// Giới hạn tần suất bằng bộ rate limiter có sẵn của ASP.NET Core: mỗi IP chỉ được N request / phút
// cho từng nhóm endpoint. Chống dò mật khẩu, spam email; không khóa tài khoản (BR15).
public static class RateLimitingExtensions
{
    private const string UnknownIpAddress = "unknown";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection(AuthRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteTooManyRequestsAsync;

            limiter.AddPolicy(
                RateLimitPolicies.Register,
                httpContext => FixedWindowPerIp(httpContext, GetOptions(httpContext).RegisterPerMinute));
            limiter.AddPolicy(
                RateLimitPolicies.Email,
                httpContext => FixedWindowPerIp(httpContext, GetOptions(httpContext).EmailPerMinute));
            limiter.AddPolicy(
                RateLimitPolicies.ResetPassword,
                httpContext => FixedWindowPerIp(httpContext, GetOptions(httpContext).ResetPasswordPerMinute));
        });

        return services;
    }

    private static AuthRateLimitOptions GetOptions(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
    }

    // Fixed window: đếm request của mỗi IP trong từng khung 1 phút, đủ số thì chặn tới hết khung.
    private static RateLimitPartition<string> FixedWindowPerIp(HttpContext httpContext, int permitLimit)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownIpAddress;

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    }

    // Trả lỗi 429 cùng định dạng ProblemDetails với ExceptionHandlingMiddleware.
    private static async ValueTask WriteTooManyRequestsAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests),
            Detail = "Too many requests. Please try again later.",
        };
        problem.Extensions["code"] = "TooManyRequests";
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
}
