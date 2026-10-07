namespace ConfHub.Application.Accounts;

// Kết quả đăng nhập / làm mới phiên mà handler trả cho controller.
// RefreshToken là chuỗi gốc: controller chỉ đặt vào cookie HttpOnly, KHÔNG trả trong body.
public sealed record SessionResult(
    string AccessToken,
    int AccessTokenExpiresInSeconds,
    CurrentUserResponse User,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
