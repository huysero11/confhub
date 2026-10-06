using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Token cùng mục đích của 1 người, chưa dùng và chưa hết hạn (điều kiện giống UserToken.IsActive).
public sealed class ActiveUserTokensSpec : Specification<UserToken>
{
    public ActiveUserTokensSpec(Guid userId, TokenPurpose purpose, DateTime now)
    {
        Query
            .Where(userToken => userToken.UserId == userId
                && userToken.Purpose == purpose
                && userToken.UsedAt == null
                && userToken.ExpiresAt > now);
    }
}
