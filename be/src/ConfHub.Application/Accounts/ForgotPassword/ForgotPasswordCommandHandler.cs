using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(IRepository<User> userRepository)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken);

        // Không có tài khoản hoặc bị khóa: không gửi gì, API vẫn trả như nhau (BR09).
        if (user is null)
        {
            return;
        }

        if (user.Status == UserStatus.Unverified)
        {
            // Chưa xác thực email thì chưa đặt lại mật khẩu được → gửi lại email xác thực thay thế.
            user.RequestEmailVerification();
        }
        else if (user.CanResetPassword)
        {
            user.RequestPasswordReset();
        }
        else
        {
            return;
        }

        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
