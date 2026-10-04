using MediatR;

namespace ConfHub.Application.Accounts.SendPasswordResetEmail;

// Gửi từ consumer RabbitMQ, không phải từ API.
public sealed record SendPasswordResetEmailCommand(Guid UserId) : IRequest;
