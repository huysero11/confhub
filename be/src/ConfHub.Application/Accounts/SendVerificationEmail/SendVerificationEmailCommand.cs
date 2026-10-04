using MediatR;

namespace ConfHub.Application.Accounts.SendVerificationEmail;

// Gửi từ consumer RabbitMQ, không phải từ API.
public sealed record SendVerificationEmailCommand(Guid UserId) : IRequest;
