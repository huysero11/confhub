using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Nạp kèm Role: đăng nhập cần vai trò và quyền để ghi vào access token.
public sealed class UserWithRoleByEmailSpec : Specification<User>
{
    public UserWithRoleByEmailSpec(string normalizedEmail)
    {
        Query.Where(user => user.Email == normalizedEmail)
            .Include(user => user.Role);
    }
}
