namespace ConfHub.Application.Accounts;

/*
- Thời hạn token và khoảng cách tối thiểu giữa 2 lần gửi email (BR08).
    Đổi ở đây nhớ sửa nội dung email.
- TimeSpan là khoảng thời gian
*/
public static class TokenLifetimes
{
    public static readonly TimeSpan VerifyEmail = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResetPassword = TimeSpan.FromMinutes(30);

    // Refresh token (giữ phiên đăng nhập). Hạn của access token nằm ở cấu hình Jwt:AccessTokenMinutes.
    public static readonly TimeSpan Refresh = TimeSpan.FromDays(7);

    // Cooldown: 2 email cùng loại gửi cho 1 người phải cách nhau ít nhất 60 giây (chống spam).
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
}
