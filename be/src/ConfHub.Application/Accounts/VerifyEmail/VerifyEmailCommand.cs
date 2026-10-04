using MediatR;

namespace ConfHub.Application.Accounts.VerifyEmail;

// Token: chuỗi lấy từ link trong email.
public sealed record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResponse>;
