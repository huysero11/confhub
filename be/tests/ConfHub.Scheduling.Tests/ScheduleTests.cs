using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Tests;

public class ScheduleTests
{
    [Fact]
    public void PlaceOverwritesPreviousPlacement()
    {
        var schedule = new Schedule();

        schedule.Place(1, 0, 10);
        schedule.Place(1, 3, 20);

        Assert.True(schedule.TryGetPlacement(1, out var placement));
        Assert.Equal(new Placement(3, 20), placement);
        Assert.Single(schedule.PlacedSessionIds);
    }

    [Fact]
    public void PlacedSessionIdsKeepPlacementOrder()
    {
        var schedule = new Schedule();

        schedule.Place(5, 0);
        schedule.Place(2, 1);
        schedule.Place(9, 2);
        schedule.Place(5, 3);

        var expectedIds = new[] { 5, 2, 9 };
        Assert.Equal(expectedIds, schedule.PlacedSessionIds.ToArray());
    }

    [Fact]
    public void SetRoomAssignsRoomToPlacedSession()
    {
        var schedule = new Schedule();
        schedule.Place(1, 4);

        schedule.SetRoom(1, 7);

        Assert.True(schedule.TryGetPlacement(1, out var placement));
        Assert.Equal(new Placement(4, 7), placement);
    }

    [Fact]
    public void SetRoomOnUnplacedSessionFails()
    {
        var schedule = new Schedule();

        Assert.Throws<InvalidOperationException>(() => schedule.SetRoom(1, 7));
    }

    [Fact]
    public void RemoveForgetsSession()
    {
        var schedule = new Schedule();
        schedule.Place(1, 0);

        schedule.Remove(1);

        Assert.False(schedule.IsPlaced(1));
        Assert.Empty(schedule.PlacedSessionIds);
    }

    [Fact]
    public void CloneIsIndependentFromOriginal()
    {
        var schedule = new Schedule();
        schedule.Place(1, 0, 10);

        var copy = schedule.Clone();
        copy.Place(1, 5, 20);
        copy.Place(2, 6);

        Assert.True(schedule.TryGetPlacement(1, out var originalPlacement));
        Assert.Equal(new Placement(0, 10), originalPlacement);
        Assert.False(schedule.IsPlaced(2));
        Assert.Equal(2, copy.PlacedSessionIds.Count);
    }
}
