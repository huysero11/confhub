using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.Logout;

// BR13: thu hồi refresh token của phiên này. Đăng xuất luôn thành công — không có cookie,
// token lạ hay đã hết hạn thì không còn gì để thu hồi.
public sealed class LogoutCommandHandler(
    IRepository<UserToken> userTokenRepository,
    TimeProvider timeProvider) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = RandomToken.Hash(request.RefreshToken);
        var userToken = await userTokenRepository.FirstOrDefaultAsync(new UserTokenByHashSpec(tokenHash), cancellationToken);
        if (userToken is null || userToken.Purpose != TokenPurpose.Refresh || !userToken.IsActive(now))
        {
            return;
        }

        userToken.MarkUsed(now);
        await userTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
