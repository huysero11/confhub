using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts;

// Cấp một phiên đăng nhập = 1 access token (JWT) + 1 refresh token. Dùng chung cho login và refresh
public sealed class SessionIssuer(
    IAccessTokenGenerator accessTokenGenerator,
    IRepository<UserToken> userTokenRepository)
{
    // Cần user.Role đã được nạp.
    public async Task<SessionResult> IssueAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        var accessToken = accessTokenGenerator.Generate(user);

        // Refresh token: chuỗi ngẫu nhiên; CSDL chỉ lưu bản băm, chuỗi gốc trả về cho controller đặt vào cookie.
        var rawRefreshToken = RandomToken.Generate();
        var refreshTokenExpiresAt = now + TokenLifetimes.Refresh;
        var refreshUserToken = UserToken.Issue(
            user.Id,
            TokenPurpose.Refresh,
            RandomToken.Hash(rawRefreshToken),
            refreshTokenExpiresAt);

        // AddAsync lưu luôn: token mới + mọi thay đổi đang chờ (vd token cũ vừa đánh dấu đã dùng) vào cùng 1 transaction.
        await userTokenRepository.AddAsync(refreshUserToken, cancellationToken);

        return new SessionResult(
            accessToken.Value,
            accessToken.ExpiresInSeconds,
            CurrentUserResponse.FromUser(user),
            rawRefreshToken,
            refreshTokenExpiresAt);
    }
}
