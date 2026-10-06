using System.ComponentModel.DataAnnotations;

namespace ConfHub.Infrastructure.Security;

// Options pattern: nhóm cấu hình "Jwt" trong appsettings, kiểm tra hợp lệ khi khởi động.
// Dùng ở 2 nơi: lúc ký token (JwtAccessTokenGenerator) và lúc kiểm tra token của request (Api).
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Issuer: ai phát hành token (API này). Audience: token dành cho ai (giao diện ConfHub).
    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    // Khóa bí mật để ký HMAC-SHA256: cần ít nhất 256 bit = 32 ký tự. Ai có khóa này tự tạo được
    // token hợp lệ → không để trong appsettings.json; máy thật đặt qua biến môi trường Jwt__SigningKey.
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 15;
}
