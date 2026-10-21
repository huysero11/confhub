using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class SchedulingProblemTests
{
    // ---------- KIỂM TRA ĐẦU VÀO ----------
    [Fact]
    public void ValidDataIsAccepted()
    {
        var problem = ExampleProblem.Create();

        Assert.Equal(4, problem.Sessions.Count);
        Assert.Equal(2, problem.Rooms.Count);
        Assert.Equal(2, problem.Timeline.Slots.Count);
    }

    [Fact]
    public void NoSessionsIsRejected()
    {
        Assert.Throws<ArgumentException>(() => TestData.CreateProblem([], [TestData.Room(1, 50)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NoRoomsIsRejected()
    {
        Assert.Throws<ArgumentException>(() => TestData.CreateProblem([TestData.Session(1, 60)], [], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NoDaysIsRejected()
    {
        Assert.Throws<ArgumentException>(() => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], []));
    }

    [Fact]
    public void DuplicateSessionIdIsRejected()
    {
        var sessions = new List<SchedulingSession> { TestData.Session(1, 60), TestData.Session(1, 60) };

        Assert.Throws<ArgumentException>(() => TestData.CreateProblem(sessions, [TestData.Room(1, 50)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void DuplicateRoomIdIsRejected()
    {
        var rooms = new List<SchedulingRoom> { TestData.Room(1, 50), TestData.Room(1, 80) };

        Assert.Throws<ArgumentException>(() => TestData.CreateProblem([TestData.Session(1, 60)], rooms, [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NonPositiveSlotMinutesIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], [TestData.Day(540, 660)], null, null, 0));
    }

    [Fact]
    public void DurationNotDivisibleBySlotIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 50)], [TestData.Room(1, 50)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NonPositiveDurationIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 0)], [TestData.Room(1, 50)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NonPositiveRoomCapacityIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 0)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void NegativeExpectedSizeIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.SessionWithSize(1, 60, -1)], [TestData.Room(1, 50)], [TestData.Day(540, 660)]));
    }

    [Fact]
    public void DayWithStartNotBeforeEndIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], [TestData.Day(660, 660)]));
    }

    [Fact]
    public void BreakOutsideDayIsRejected()
    {
        var day = TestData.Day(540, 660, new TimeInterval(650, 700));

        Assert.Throws<ArgumentException>(() => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], [day]));
    }

    [Fact]
    public void SpeakerAvailabilityWithUnknownDayIsRejected()
    {
        List<SpeakerAvailability> availabilities = [new SpeakerAvailability(1, 5, 540, 600)];

        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60, 1)], [TestData.Room(1, 50)], [TestData.Day(540, 660)], availabilities));
    }

    [Fact]
    public void SpeakerAvailabilityWithStartNotBeforeEndIsRejected()
    {
        List<SpeakerAvailability> availabilities = [new SpeakerAvailability(1, 0, 600, 600)];

        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60, 1)], [TestData.Room(1, 50)], [TestData.Day(540, 660)], availabilities));
    }

    [Fact]
    public void InterestInUnknownSessionIsRejected()
    {
        List<InterestEntry> interests = [new InterestEntry(1, 99)];

        Assert.Throws<ArgumentException>(
            () => TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], [TestData.Day(540, 660)], null, interests));
    }

    // ---------- QUY MÔ PHIÊN ----------
    [Fact]
    public void ExpectedSizeTakesPriorityOverInterestCount()
    {
        var problem = ExampleProblem.Create();

        // Phiên A khai 42 chỗ trong khi có 40 người quan tâm.
        Assert.Equal(42, problem.GetSessionSize(ExampleProblem.SessionA));
    }

    [Fact]
    public void MissingExpectedSizeFallsBackToInterestCount()
    {
        List<InterestEntry> interests = [new InterestEntry(1, 1), new InterestEntry(2, 1), new InterestEntry(3, 1)];
        var problem = TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 50)], [TestData.Day(540, 660)], null, interests);

        Assert.Equal(3, problem.GetSessionSize(1));
    }

    // ---------- MỐC BẮT ĐẦU HỢP LỆ (H5) ----------
    [Fact]
    public void SessionWithoutSpeakerAvailabilityCanStartAtEveryUsableSlot()
    {
        var problem = TestData.CreateProblem([TestData.Session(1, 60, 7)], [TestData.Room(1, 50)], [TestData.Day(540, 660)]);

        // Slot 15 phút, phiên 60 phút: bắt đầu được ở 9:00, 9:15, ..., 10:00 (5 mốc).
        Assert.Equal(5, problem.GetValidStartSlots(1).Count);
    }

    [Fact]
    public void SessionCannotStartWhereItWouldCrossBreakOrDayEnd()
    {
        List<SchedulingDay> days = [TestData.Day(8 * 60, 17 * 60, new TimeInterval(690, 780))];
        var problem = TestData.CreateProblem([TestData.Session(1, 90)], [TestData.Room(1, 50)], days);

        Assert.False(problem.IsValidStartSlot(1, TestData.SlotIndexAt(problem, 0, 660)));
        Assert.False(problem.IsValidStartSlot(1, TestData.SlotIndexAt(problem, 0, 1005)));
        Assert.True(problem.IsValidStartSlot(1, TestData.SlotIndexAt(problem, 0, 600)));
        Assert.True(problem.IsValidStartSlot(1, TestData.SlotIndexAt(problem, 0, 780)));
    }

    [Fact]
    public void SpeakerWithAvailabilityOnlyAllowsSessionsInsideIt()
    {
        List<SpeakerAvailability> availabilities = [new SpeakerAvailability(7, 0, 540, 600)];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 7)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 720)],
            availabilities);

        // Chỉ khoảng 9:00-10:00 được khai, phiên 60 phút chỉ bắt đầu được lúc 9:00.
        var validSlots = problem.GetValidStartSlots(1);
        var onlySlot = Assert.Single(validSlots);
        Assert.Equal(TestData.SlotIndexAt(problem, 0, 540), onlySlot);
    }

    [Fact]
    public void AdjacentAvailabilityIntervalsAreMerged()
    {
        // 9:00-10:00 và 10:00-11:00 liền nhau: phiên 60 phút bắt đầu 9:30 vắt qua điểm nối vẫn hợp lệ.
        List<SpeakerAvailability> availabilities =
        [
            new SpeakerAvailability(7, 0, 540, 600),
            new SpeakerAvailability(7, 0, 600, 660),
        ];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 7)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 720)],
            availabilities);

        Assert.True(problem.IsValidStartSlot(1, TestData.SlotIndexAt(problem, 0, 570)));
    }

    [Fact]
    public void DayWithoutAvailabilityIsNotAllowedOnceSpeakerDeclaredAny()
    {
        List<SpeakerAvailability> availabilities = [new SpeakerAvailability(7, 0, 540, 660)];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 7)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 660), TestData.Day(540, 660)],
            availabilities);

        foreach (var slotIndex in problem.GetValidStartSlots(1))
        {
            Assert.Equal(0, problem.Timeline.Slots[slotIndex].DayIndex);
        }

        Assert.NotEmpty(problem.GetValidStartSlots(1));
    }

    [Fact]
    public void AllSpeakersOfASessionMustBeFree()
    {
        List<SpeakerAvailability> availabilities =
        [
            new SpeakerAvailability(7, 0, 540, 660),
            new SpeakerAvailability(8, 0, 600, 660),
        ];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 7, 8)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 660)],
            availabilities);

        // Diễn giả 8 chỉ rảnh 10:00-11:00 nên phiên chỉ bắt đầu được lúc 10:00.
        var onlySlot = Assert.Single(problem.GetValidStartSlots(1));
        Assert.Equal(TestData.SlotIndexAt(problem, 0, 600), onlySlot);
    }

    [Fact]
    public void GetSessionIntervalUsesSessionDuration()
    {
        var problem = ExampleProblem.Create();

        var (dayIndex, interval) = problem.GetSessionInterval(ExampleProblem.SessionA, ExampleProblem.TenOClock);

        Assert.Equal(0, dayIndex);
        Assert.Equal(new TimeInterval(600, 660), interval);
    }
}
