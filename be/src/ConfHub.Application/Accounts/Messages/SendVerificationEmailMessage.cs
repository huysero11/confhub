namespace ConfHub.Application.Accounts.Messages;

// Message qua RabbitMQ: chỉ mang UserId, không mang token (BR07).
public sealed record SendVerificationEmailMessage(Guid UserId);
