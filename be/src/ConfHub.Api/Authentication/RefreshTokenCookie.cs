namespace ConfHub.Api.Authentication;

// Cookie chứa refresh token (BR11).
// - HttpOnly: JavaScript không đọc được → script độc hại (XSS) không lấy trộm được.
// - Secure: chỉ gửi qua HTTPS (trình duyệt coi http://localhost là an toàn nên máy dev vẫn chạy).
// - SameSite=Strict: trang web khác không khiến trình duyệt gửi kèm cookie này được (chống CSRF).
// - Path=/api/auth: chỉ gửi kèm các request đăng nhập / làm mới / đăng xuất, không gửi cho API khác.
public static class RefreshTokenCookie
{
    public const string Name = "confhub_refresh";

    private const string CookiePath = "/api/auth";

    public static string? Read(HttpRequest request)
    {
        return request.Cookies[Name];
    }

    public static void Append(HttpResponse response, string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = BuildCookieOptions();
        cookieOptions.Expires = new DateTimeOffset(expiresAt, TimeSpan.Zero);
        response.Cookies.Append(Name, refreshToken, cookieOptions);
    }

    // Xóa cookie = gửi lại cookie cùng tên, cùng Path với hạn trong quá khứ.
    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, BuildCookieOptions());
    }

    private static CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
        };
    }
}
