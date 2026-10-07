using MediatR;

namespace ConfHub.Application.Accounts.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<SessionResult>;
