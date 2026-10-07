using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Token cùng mục đích được tạo gần nhất → biết lần gửi email trước là khi nào (BR08).
public sealed class LatestUserTokenSpec : Specification<UserToken>
{
    public LatestUserTokenSpec(Guid userId, TokenPurpose purpose)
    {
        Query
            .Where(userToken => userToken.UserId == userId && userToken.Purpose == purpose)
            .OrderByDescending(userToken => userToken.CreatedAt);
    }
}
