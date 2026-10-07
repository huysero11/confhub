using System.Threading.RateLimiting;
using ConfHub.Api.ErrorHandling;
using Microsoft.AspNetCore.RateLimiting;
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
                RateLimitPolicies.Login,
                httpContext => CreateFixedWindowPartition(httpContext, GetRateLimitOptions(httpContext).LoginPerMinute));
            limiter.AddPolicy(
                RateLimitPolicies.Register,
                httpContext => CreateFixedWindowPartition(httpContext, GetRateLimitOptions(httpContext).RegisterPerMinute));
            limiter.AddPolicy(
                RateLimitPolicies.Email,
                httpContext => CreateFixedWindowPartition(httpContext, GetRateLimitOptions(httpContext).EmailPerMinute));
            limiter.AddPolicy(
                RateLimitPolicies.ResetPassword,
                httpContext => CreateFixedWindowPartition(httpContext, GetRateLimitOptions(httpContext).ResetPasswordPerMinute));
        });

        return services;
    }

    private static AuthRateLimitOptions GetRateLimitOptions(HttpContext httpContext)
    {
        return httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
    }

    // Fixed window: đếm request của mỗi IP trong từng khung 1 phút, đủ số thì chặn tới hết khung.
    private static RateLimitPartition<string> CreateFixedWindowPartition(HttpContext httpContext, int permitLimit)
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
        await ProblemResponseWriter.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "TooManyRequests",
            "Too many requests. Please try again later.",
            cancellationToken);
    }
}
