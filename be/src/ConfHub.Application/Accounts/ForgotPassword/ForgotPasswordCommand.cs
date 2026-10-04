using MediatR;

namespace ConfHub.Application.Accounts.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest;
