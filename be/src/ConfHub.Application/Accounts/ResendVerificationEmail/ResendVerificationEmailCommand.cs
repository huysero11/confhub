using MediatR;

namespace ConfHub.Application.Accounts.ResendVerificationEmail;

public sealed record ResendVerificationEmailCommand(string Email) : IRequest;
