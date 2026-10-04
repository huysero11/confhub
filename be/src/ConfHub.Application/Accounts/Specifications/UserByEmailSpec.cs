using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

public sealed class UserByEmailSpec : Specification<User>
{
    public UserByEmailSpec(string normalizedEmail)
    {
        Query.Where(user => user.Email == normalizedEmail);
    }
}
