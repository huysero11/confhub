using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.RefreshSession;

// BR12 — xoay vòng refresh token: mỗi token chỉ đổi được 1 lần lấy cặp token mới.
public sealed class RefreshSessionCommandHandler(
    IRepository<UserToken> userTokenRepository,
    IReadRepository<User> userRepository,
    SessionIssuer sessionIssuer,
    TimeProvider timeProvider) : IRequestHandler<RefreshSessionCommand, SessionResult>
{
    public async Task<SessionResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw CreateSessionExpiredException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = RandomToken.Hash(request.RefreshToken);
        var userToken = await userTokenRepository.FirstOrDefaultAsync(new UserTokenByHashSpec(tokenHash), cancellationToken);
        if (userToken is null || userToken.Purpose != TokenPurpose.Refresh)
        {
            throw CreateSessionExpiredException();
        }

        if (userToken.UsedAt is not null)
        {
            // Token đã dùng mà bị gửi lại = có người giữ bản sao (dấu hiệu bị đánh cắp).
            // Không biết ai là chủ thật → thu hồi mọi phiên, bắt đăng nhập lại.
            await RevokeAllSessionsAsync(userToken.UserId, now, cancellationToken);
            throw CreateSessionExpiredException();
        }

        if (!userToken.IsActive(now))
        {
            throw CreateSessionExpiredException();
        }

        var user = await userRepository.FirstOrDefaultAsync(new UserWithRoleByIdSpec(userToken.UserId), cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            throw CreateSessionExpiredException();
        }

        // Token cũ (đánh dấu đã dùng) và token mới được lưu trong cùng 1 lần SaveChanges của IssueAsync.
        userToken.MarkUsed(now);
        return await sessionIssuer.IssueAsync(user, now, cancellationToken);
    }

    // Mọi lý do thất bại trả cùng 1 lỗi; giao diện chỉ cần biết "phải đăng nhập lại".
    private static UnauthorizedException CreateSessionExpiredException()
    {
        return new UnauthorizedException(AccountErrorCodes.SessionExpired, "Session expired. Please log in again.");
    }

    private async Task RevokeAllSessionsAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        // Có nhiều refresh token đang active vì có thể đăng nhập ở nhiều thiết bị, mỗi cái sẽ có một token đang active
        var activeRefreshTokens = await userTokenRepository.ListAsync(new ActiveUserTokensSpec(userId, TokenPurpose.Refresh, now), cancellationToken);
        foreach (var activeRefreshToken in activeRefreshTokens)
        {
            activeRefreshToken.MarkUsed(now);
        }

        await userTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
