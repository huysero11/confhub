using System.ComponentModel.DataAnnotations;

namespace ConfHub.Infrastructure.Email;

// Options pattern: nhóm cấu hình "Smtp" trong appsettings, kiểm tra hợp lệ khi khởi động.
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    // Máy chủ mail thật cần mã hóa + đăng nhập; smtp4dev (dev) thì không.
    public bool UseStartTls { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = string.Empty;
}
