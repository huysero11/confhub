using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Token cùng mục đích của 1 người, chưa dùng và chưa hết hạn (điều kiện giống UserToken.IsActive).
public sealed class ActiveTokensSpec : Specification<UserToken>
{
    public ActiveTokensSpec(Guid userId, TokenPurpose purpose, DateTime now)
    {
        Query
            .Where(token => token.UserId == userId
                && token.Purpose == purpose
                && token.UsedAt == null
                && token.ExpiresAt > now);
    }
}
