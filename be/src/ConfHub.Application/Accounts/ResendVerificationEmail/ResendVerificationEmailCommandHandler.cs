using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.ResendVerificationEmail;

public sealed class ResendVerificationEmailCommandHandler(IRepository<User> userRepository)
    : IRequestHandler<ResendVerificationEmailCommand>
{
    public async Task Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);

        // Không có tài khoản hoặc đã xác thực: không làm gì. API vẫn trả như nhau
        // để người ngoài không dò được email nào có tài khoản (BR09).
        if (user is null || user.Status != UserStatus.Unverified)
        {
            return;
        }

        // Chỉ ghi sự kiện; consumer mới áp BR08 (60 giây) và gửi email.
        user.RequestEmailVerification();
        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
