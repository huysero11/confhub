namespace ConfHub.Infrastructure.IntegrationTests.Messaging;

// Tin nhắn mẫu chỉ dùng trong test để thử đường ống sự kiện (chưa có sự kiện nghiệp vụ nào).
public sealed record PingMessage(Guid Id);
