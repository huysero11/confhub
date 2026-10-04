using ConfHub.Application.Common.Email;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.SendPasswordResetEmail;

// Giống SendVerificationEmailCommandHandler, khác mục đích token và mẫu email.
public sealed class SendPasswordResetEmailCommandHandler(
    IRepository<User> userRepository,
    EmailTokenIssuer emailTokenIssuer,
    IEmailSender emailSender) : IRequestHandler<SendPasswordResetEmailCommand>
{
    public async Task Handle(SendPasswordResetEmailCommand request, CancellationToken cancellationToken)
    {
        // Bước 1: bị khóa trong lúc chờ thì không gửi.
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.CanResetPassword)
        {
            return;
        }

        // Bước 2–4.
        var rawToken = await emailTokenIssuer.IssueAsync(
            user.Id,
            TokenPurpose.ResetPassword,
            TokenLifetimes.ResetPassword,
            cancellationToken);
        if (rawToken is null)
        {
            return;
        }

        // Bước 5.
        await emailSender.SendPasswordResetEmailAsync(user.Email, user.FullName, rawToken, cancellationToken);
    }
}
