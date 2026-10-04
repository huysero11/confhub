using ConfHub.Application.Common.Email;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.SendVerificationEmail;

// Các bước: docs/specs/g1-account.md mục 7
// nhận từ consumer
public sealed class SendVerificationEmailCommandHandler(
    IRepository<User> userRepository,
    EmailTokenIssuer emailTokenIssuer,
    IEmailSender emailSender
) : IRequestHandler<SendVerificationEmailCommand>
{
    public async Task Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Unverified)
        {
            // Không gửi email nếu user không tồn tại hoặc đã xác thực.
            return;
        }

        var rawToken = await emailTokenIssuer.IssueAsync(
            user.Id,
            TokenPurpose.VerifyEmail,
            TokenLifetimes.VerifyEmail,
            cancellationToken);
        if (rawToken is null)
        {
            // null = vừa gửi email cùng loại trong 60 giây (cooldown) → không gửi nữa.
            return;
        }

        await emailSender.SendVerificationEmailAsync(
            user.Email,
            user.FullName,
            rawToken,
            cancellationToken);
    }
}
