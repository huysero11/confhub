using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using ConfHub.Domain.Common;
using MediatR;

namespace ConfHub.Application.Accounts.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IRepository<UserToken> userTokenRepository,
    IRepository<User> userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var tokenHash = RandomToken.Hash(request.Token);
        var userToken = await userTokenRepository.FirstOrDefaultAsync(new TokenByHashSpec(tokenHash), cancellationToken);
        if (userToken is null || userToken.Purpose != TokenPurpose.ResetPassword || !userToken.IsActive(now))
        {
            throw TokenInvalid();
        }

        var user = await userRepository.GetByIdAsync(userToken.UserId, cancellationToken);
        if (user is null || !user.CanResetPassword)
        {
            throw TokenInvalid();
        }

        user.ResetPassword(passwordHasher.Hash(request.NewPassword));
        userToken.MarkUsed(now);

        // BR14: đổi mật khẩu → đăng xuất khỏi mọi thiết bị (thu hồi refresh token còn hạn).
        var refreshTokens = await userTokenRepository.ListAsync(
            new ActiveTokensSpec(user.Id, TokenPurpose.Refresh, now),
            cancellationToken);
        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.MarkUsed(now);
        }

        // Mọi thay đổi ở trên cùng 1 DbContext → 1 lần SaveChanges = 1 transaction.
        await userTokenRepository.SaveChangesAsync(cancellationToken);
    }

    private static DomainException TokenInvalid()
    {
        return new DomainException(AccountErrorCodes.TokenInvalid, "The link is invalid or has expired.");
    }
}
