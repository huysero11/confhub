using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Tests;

public class TimeIntervalTests
{
    [Fact]
    public void IntervalsSharingTimeOverlap()
    {
        var first = new TimeInterval(540, 630);
        var second = new TimeInterval(600, 660);

        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Fact]
    public void IntervalsTouchingAtEndpointDoNotOverlap()
    {
        var first = new TimeInterval(540, 600);
        var second = new TimeInterval(600, 660);

        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void ContainsRequiresWholeIntervalInside()
    {
        var outer = new TimeInterval(540, 660);

        Assert.True(outer.Contains(new TimeInterval(540, 600)));
        Assert.True(outer.Contains(outer));
        Assert.False(outer.Contains(new TimeInterval(600, 700)));
        Assert.Equal(120, outer.Length);
    }
}
