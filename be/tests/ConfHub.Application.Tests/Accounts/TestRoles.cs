using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Tests.Accounts;

internal static class TestRoles
{
    public static Role Create(string code)
    {
        return Role.Create(Guid.NewGuid(), code, code);
    }
}
