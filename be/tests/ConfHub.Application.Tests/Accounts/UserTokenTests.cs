using ConfHub.Domain.Accounts;
using ConfHub.Domain.Common;

namespace ConfHub.Application.Tests.Accounts;

public class UserTokenTests
{
    private static readonly DateTime _now = new(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewTokenIsActiveUntilExpiry()
    {
        var token = UserToken.Issue(Guid.NewGuid(), TokenPurpose.VerifyEmail, "hash", _now.AddHours(24));

        Assert.True(token.IsActive(_now));
        Assert.False(token.IsActive(_now.AddHours(24)));
    }

    [Fact]
    public void UsedTokenIsNotActiveAndCannotBeUsedAgain()
    {
        var token = UserToken.Issue(Guid.NewGuid(), TokenPurpose.VerifyEmail, "hash", _now.AddHours(24));

        token.MarkUsed(_now);

        Assert.Equal(_now, token.UsedAt);
        Assert.False(token.IsActive(_now));
        Assert.Throws<DomainException>(() => token.MarkUsed(_now));
    }
}
