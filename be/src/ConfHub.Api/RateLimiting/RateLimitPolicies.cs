namespace ConfHub.Api.RateLimiting;

// Tên các policy giới hạn tần suất, gắn lên endpoint bằng [EnableRateLimiting(...)].
public static class RateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Register = "auth-register";
    public const string Email = "auth-email";
    public const string ResetPassword = "auth-reset-password";
}
