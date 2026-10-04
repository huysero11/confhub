using ConfHub.Application.Common.Security;

namespace ConfHub.Application.Tests.Common.Security;

public class RandomTokenTests
{
    [Fact]
    public void GenerateReturnsDifferentUrlSafeTokens()
    {
        var first = RandomToken.Generate();
        var second = RandomToken.Generate();

        Assert.NotEqual(first, second);
        Assert.Equal(43, first.Length);
        Assert.DoesNotContain('+', first);
        Assert.DoesNotContain('/', first);
        Assert.DoesNotContain('=', first);
    }

    [Fact]
    public void HashIsStableHexOf64Characters()
    {
        var hash = RandomToken.Hash("abc");

        Assert.Equal(hash, RandomToken.Hash("abc"));
        Assert.Equal(64, hash.Length);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
    }
}
