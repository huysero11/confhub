using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts;

// Bước 2–4 của consumer gửi email (docs/specs/g1-account.md mục 7), dùng chung cho
// email xác thực và email đặt lại mật khẩu.
public sealed class EmailTokenIssuer(
    IRepository<UserToken> userTokenRepository,
    TimeProvider timeProvider)
{
    // Trả token gốc để đưa vào email; trả null nếu đang trong thời gian cooldown.
    public async Task<string?> IssueAsync(
        Guid userId,
        TokenPurpose purpose,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var latestUserToken = await userTokenRepository.FirstOrDefaultAsync(
            new LatestTokenSpec(userId, purpose),
            cancellationToken);

        // Cooldown (BR08): vừa gửi email cùng loại trong vòng 60 giây thì không gửi nữa.
        if (latestUserToken is not null && now - TokenLifetimes.ResendCooldown < latestUserToken.CreatedAt)
        {
            return null;
        }

        // các link trong email bị cũ, đánh dấu đã dùng, chỉ cái mới nhất dùng được
        var oldUserTokens = await userTokenRepository.ListAsync(
            new ActiveTokensSpec(userId, purpose, now),
            cancellationToken);
        foreach (var oldUserToken in oldUserTokens)
        {
            oldUserToken.MarkUsed(now);
        }

        // AddAsync lưu cả token mới lẫn các token cũ vừa thu hồi trong 1 lần SaveChanges
        var rawToken = RandomToken.Generate();
        var newUserToken = UserToken.Issue(userId, purpose, RandomToken.Hash(rawToken), now + lifetime);
        await userTokenRepository.AddAsync(newUserToken, cancellationToken);

        return rawToken;
    }
}
