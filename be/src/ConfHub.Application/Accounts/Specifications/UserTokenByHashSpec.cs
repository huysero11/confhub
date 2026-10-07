using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

public sealed class UserTokenByHashSpec : Specification<UserToken>
{
    public UserTokenByHashSpec(string tokenHash)
    {
        Query.Where(userToken => userToken.TokenHash == tokenHash);
    }
}
