using System.ComponentModel.DataAnnotations;

namespace ConfHub.Api.RateLimiting;

// Options pattern: số request tối đa mỗi phút cho mỗi IP (BR15), đọc từ mục "RateLimiting".
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10000)]
    public int RegisterPerMinute { get; init; } = 5;

    // Gửi lại email xác thực + quên mật khẩu.
    [Range(1, 10000)]
    public int EmailPerMinute { get; init; } = 3;

    [Range(1, 10000)]
    public int ResetPasswordPerMinute { get; init; } = 5;
}
