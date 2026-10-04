using Ardalis.Specification;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts.Specifications;

public sealed class TokenByHashSpec : Specification<UserToken>
{
    public TokenByHashSpec(string tokenHash)
    {
        Query.Where(token => token.TokenHash == tokenHash);
    }
}
