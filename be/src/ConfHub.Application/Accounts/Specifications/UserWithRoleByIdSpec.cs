using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

// Nạp kèm Role: User.VerifyEmail() cần biết vai trò để chọn trạng thái tiếp theo.
public sealed class UserWithRoleByIdSpec : Specification<User>
{
    public UserWithRoleByIdSpec(Guid userId)
    {
        Query.Where(user => user.Id == userId)
            .Include(user => user.Role);
    }
}
