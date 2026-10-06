using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.Login;

// BR10. Thứ tự kiểm tra quan trọng: mật khẩu trước, trạng thái sau.
public sealed class LoginCommandHandler(
    IReadRepository<User> userRepository,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, SessionResult>
{
    public async Task<SessionResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await userRepository.FirstOrDefaultAsync(new UserWithRoleByEmailSpec(email), cancellationToken);
        if (user is null)
        {
            // Vẫn băm 1 lần để thời gian trả lời giống trường hợp có tài khoản
            // → người lạ không đoán được email nào tồn tại qua độ nhanh chậm của response.
            _ = passwordHasher.Hash(request.Password);
            throw CreateInvalidCredentialsException();
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw CreateInvalidCredentialsException();
        }

        // Mật khẩu đúng rồi mới nói về trạng thái → người lạ không dò được tài khoản đang bị khóa hay chờ duyệt.
        EnsureActive(user);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // IssueAsync sẽ tạo access token + refresh token, lưu refresh token vào CSDL, trả về SessionResult.
        return await sessionIssuer.IssueAsync(user, now, cancellationToken);
    }

    private static UnauthorizedException CreateInvalidCredentialsException()
    {
        // Sai email và sai mật khẩu trả cùng 1 lỗi.
        return new UnauthorizedException(AccountErrorCodes.InvalidCredentials, "Invalid email or password.");
    }

    private static void EnsureActive(User user)
    {
        if (user.Status == UserStatus.Active)
        {
            return;
        }

        if (user.Status == UserStatus.Unverified)
        {
            throw new ForbiddenException(AccountErrorCodes.AccountUnverified, "Email has not been verified.");
        }

        if (user.Status == UserStatus.PendingApproval)
        {
            throw new ForbiddenException(AccountErrorCodes.AccountPending, "Account is waiting for approval.");
        }

        throw new ForbiddenException(AccountErrorCodes.AccountLocked, "The account is locked.");
    }
}
