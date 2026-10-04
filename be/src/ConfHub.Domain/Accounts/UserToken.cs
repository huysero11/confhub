using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts;

public sealed class UserToken : BaseEntity, IAggregateRoot
{
    private UserToken()
    {
    }

    public Guid UserId { get; private set; }
    public TokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }

    // null = chưa dùng; có giá trị = đã dùng hoặc đã bị thu hồi → hết hiệu lực.
    public DateTime? UsedAt { get; private set; }

    public static UserToken Issue(Guid userId, TokenPurpose purpose, string tokenHash, DateTime expiresAt)
    {
        return new UserToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
    }

    public bool IsActive(DateTime now)
    {
        return UsedAt is null && ExpiresAt > now;
    }

    public void MarkUsed(DateTime now)
    {
        if (UsedAt is not null)
        {
            throw new DomainException(
                AccountErrorCodes.TokenAlreadyUsed,
                "The token has already been used.");
        }

        UsedAt = now;
    }
}
