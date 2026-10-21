using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class TimelineTests
{
    [Fact]
    public void SlotCountDependsOnSlotMinutes()
    {
        List<SchedulingDay> days = [TestData.Day(9 * 60, 11 * 60)];

        Assert.Equal(8, new Timeline(days, 15).Slots.Count);
        Assert.Equal(2, new Timeline(days, 60).Slots.Count);
    }

    [Fact]
    public void LunchBreakRemovesSlotsInsideIt()
    {
        // Nghỉ trưa 11:30-13:00: mốc 11:15 còn, mốc 11:30 mất, mốc 12:45 mất, mốc 13:00 còn lại.
        List<SchedulingDay> days = [TestData.Day(8 * 60, 17 * 60, new TimeInterval(690, 780))];

        var timeline = new Timeline(days, 15);
        var startMinutes = timeline.Slots.Select(slot => slot.StartMinute).ToList();

        Assert.Contains(675, startMinutes);
        Assert.DoesNotContain(690, startMinutes);
        Assert.DoesNotContain(765, startMinutes);
        Assert.Contains(780, startMinutes);
        Assert.Equal(30, timeline.Slots.Count);
    }

    [Fact]
    public void BreakNotAlignedToSlotsStillRemovesTheSlotItTouches()
    {
        // Nghỉ 9:30-10:00, mốc 60 phút: mốc 9:00 [540, 600) giao khung nghỉ nên mất; mốc 10:00 chỉ chạm đầu mút nên còn.
        List<SchedulingDay> days = [TestData.Day(540, 660, new TimeInterval(570, 600))];

        var timeline = new Timeline(days, 60);

        var slot = Assert.Single(timeline.Slots);
        Assert.Equal(600, slot.StartMinute);
    }

    [Fact]
    public void SlotIndexIsContinuousAcrossDays()
    {
        List<SchedulingDay> days = [TestData.Day(540, 660), TestData.Day(540, 660)];

        var timeline = new Timeline(days, 60);

        Assert.Equal(4, timeline.Slots.Count);
        for (var index = 0; index < timeline.Slots.Count; index++)
        {
            Assert.Equal(index, timeline.Slots[index].Index);
        }

        Assert.Equal(0, timeline.Slots[1].DayIndex);
        Assert.Equal(1, timeline.Slots[2].DayIndex);
    }

    [Fact]
    public void CanHostRejectsSessionCrossingBreakOrDayEnd()
    {
        List<SchedulingDay> days = [TestData.Day(8 * 60, 17 * 60, new TimeInterval(690, 780))];
        var problem = TestData.CreateProblem([TestData.Session(1, 90)], [TestData.Room(1, 50)], days);
        var timeline = problem.Timeline;

        var tenOClock = TestData.SlotIndexAt(problem, 0, 600);
        var elevenOClock = TestData.SlotIndexAt(problem, 0, 660);
        var lastSlot = TestData.SlotIndexAt(problem, 0, 1005);
        var oneOClock = TestData.SlotIndexAt(problem, 0, 780);

        Assert.True(timeline.CanHost(tenOClock, 90));
        Assert.False(timeline.CanHost(elevenOClock, 90));
        Assert.False(timeline.CanHost(lastSlot, 90));
        Assert.True(timeline.CanHost(oneOClock, 90));
        Assert.False(timeline.CanHost(-1, 90));
    }
}
