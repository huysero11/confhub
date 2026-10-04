using MediatR;

namespace ConfHub.Application.Accounts.Register;

public sealed record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string Role,
    string? Organization) : IRequest<RegisterResponse>;
