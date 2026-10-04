using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Token cùng mục đích được tạo gần nhất → biết lần gửi email trước là khi nào (BR08).
public sealed class LatestTokenSpec : Specification<UserToken>
{
    public LatestTokenSpec(Guid userId, TokenPurpose purpose)
    {
        Query
            .Where(token => token.UserId == userId && token.Purpose == purpose)
            .OrderByDescending(token => token.CreatedAt);
    }
}
