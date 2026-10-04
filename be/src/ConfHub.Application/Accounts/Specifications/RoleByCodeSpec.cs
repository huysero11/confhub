using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

public sealed class RoleByCodeSpec : Specification<Role>
{
    public RoleByCodeSpec(string code)
    {
        Query.Where(role => role.Code == code);
    }
}
