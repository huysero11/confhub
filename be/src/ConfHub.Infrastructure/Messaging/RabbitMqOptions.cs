using System.ComponentModel.DataAnnotations;

namespace ConfHub.Infrastructure.Messaging;

// Options pattern: gom các cấu hình RabbitMQ (mục "RabbitMq" trong appsettings) vào một lớp có kiểu rõ ràng.
// [Required]: thiếu hoặc để rỗng thì API không khởi động được và báo lỗi rõ ràng (ValidateOnStart).
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
