using MediatR;

namespace ConfHub.Application.Accounts.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
