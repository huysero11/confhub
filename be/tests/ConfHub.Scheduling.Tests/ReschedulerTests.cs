using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rescheduling;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm bốn bước xếp lại bằng ví dụ tay 3.6 và các biến động trên dữ liệu sinh.
public class ReschedulerTests
{
    // ---------- VÍ DỤ TÍNH TAY ----------
    // Dùng ở: dotnet test / Debug Test (B bị H5, C nhường chỗ; điểm cố định theo đặc tả).
    [Fact]
    public void ExampleWithoutLocksDisplacesBAndMovesCToPlaceEverySession()
    {
        var problem = CreateChangedExample();
        var baseSchedule = ExampleProblem.CreateSwappedSchedule();
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule);

        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateGreedySchedule(), rescheduleResult.Schedule);
        Assert.Empty(rescheduleResult.UnplacedSessions);
        Assert.Equal([3, 4], rescheduleResult.ChangedSessionIds);
        var displacedSession = Assert.Single(rescheduleResult.DisplacedSessions);
        Assert.Equal(ExampleProblem.SessionB, displacedSession.SessionId);
        Assert.Equal([ConstraintCode.H5], displacedSession.Codes);
        Assert.Equal("Phiên 3 phải dời: mốc không hợp lệ (H5).", displacedSession.Reason);
        Assert.Equal(0.9202465, rescheduleResult.Value, 7);
        Assert.Empty(new ConstraintChecker(problem).Validate(rescheduleResult.Schedule));
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), baseSchedule);
    }

    // Dùng ở: dotnet test / Debug Test (C ghim làm bước 3 không cứu được B; giữ lý do H2 của tham lam).
    [Fact]
    public void ExampleWithCLockedLeavesBUnplacedWithH2()
    {
        var problem = CreateChangedExample();
        var baseSchedule = ExampleProblem.CreateSwappedSchedule();
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule, new HashSet<int> { ExampleProblem.SessionC });
        var expectedSchedule = baseSchedule.Clone();
        expectedSchedule.Remove(ExampleProblem.SessionB);

        LocalSearchTests.AssertSameSchedule(expectedSchedule, rescheduleResult.Schedule);
        Assert.Equal([3], rescheduleResult.ChangedSessionIds);
        var unplacedSession = Assert.Single(rescheduleResult.UnplacedSessions);
        Assert.Equal(ExampleProblem.SessionB, unplacedSession.SessionId);
        Assert.Single(unplacedSession.BlockedSlotCounts);
        Assert.Equal(1, unplacedSession.BlockedSlotCounts[ConstraintCode.H2]);
        Assert.Contains("hết phòng (H2) ở 1 mốc", unplacedSession.Reason);
        Assert.Equal(0.2823944, rescheduleResult.Value, 7);
        Assert.Empty(new ConstraintChecker(problem).Validate(rescheduleResult.Schedule));
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), baseSchedule);
    }

    // ---------- KHÔNG BIẾN ĐỘNG VÀ KIỂM ĐẦU VÀO ----------
    // Dùng ở: dotnet test (lịch gốc M3 giữ y nguyên khi bài toán không thay đổi).
    [Fact]
    public void UnchangedProblemKeepsOriginalM3Schedule()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Small).Problem;
        var baseSchedule = new LocalSearchScheduler(problem).Run().Schedule;
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule);
        LocalSearchTests.AssertSameSchedule(baseSchedule, rescheduleResult.Schedule);
        Assert.Empty(rescheduleResult.DisplacedSessions);
        Assert.Empty(rescheduleResult.ChangedSessionIds);
        Assert.Equal(new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(baseSchedule).Value, rescheduleResult.Value);
    }

    // Dùng ở: dotnet test (lịch thiếu phòng hoặc tập ghim chứa phiên không có trong lịch gốc phải báo lỗi).
    [Fact]
    public void MissingRoomAndUnknownLockAreRejected()
    {
        var scheduler = new Rescheduler(ExampleProblem.Create());
        var baseSchedule = ExampleProblem.CreateSwappedSchedule();
        Assert.Throws<ArgumentException>(() => scheduler.Run(baseSchedule, new HashSet<int> { 99 }));
        baseSchedule.SetRoom(ExampleProblem.SessionA, null);
        Assert.Throws<ArgumentException>(() => scheduler.Run(baseSchedule));
        var deletedRoomlessSession = new Schedule();
        deletedRoomlessSession.Place(99, 0);
        Assert.Throws<ArgumentException>(() => scheduler.Run(deletedRoomlessSession));
    }

    // Dùng ở: dotnet test (ghim hết hợp lệ mất ghim rồi được xếp lại; vẫn ghi rõ phiên bị gỡ).
    [Fact]
    public void InvalidLockedSessionIsReleasedAndRescheduled()
    {
        var rescheduleResult = new Rescheduler(CreateChangedExample()).Run(
            ExampleProblem.CreateSwappedSchedule(), new HashSet<int> { ExampleProblem.SessionB });
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateGreedySchedule(), rescheduleResult.Schedule);
        Assert.Equal(ExampleProblem.SessionB, Assert.Single(rescheduleResult.DisplacedSessions).SessionId);
        Assert.Equal([3, 4], rescheduleResult.ChangedSessionIds);
    }

    // Dùng ở: dotnet test (ghim được giữ trước phiên không ghim dù đứng sau trong thứ tự nhập).
    [Fact]
    public void LockedSessionHasPriorityWhenOldPlacementsConflict()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10), TestData.Session(2, 60, 10)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var baseSchedule = new Schedule();
        baseSchedule.Place(1, 0, 1);
        baseSchedule.Place(2, 0, 2);
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule, new HashSet<int> { 2 });
        var displaced = Assert.Single(rescheduleResult.DisplacedSessions);
        Assert.Equal(1, displaced.SessionId);
        Assert.Equal([ConstraintCode.H1], displaced.Codes);
        Assert.True(rescheduleResult.Schedule.TryGetPlacement(2, out var lockedPlacement));
        Assert.Equal(new Placement(0, 2), lockedPlacement);
        Assert.Empty(new ConstraintChecker(problem).Validate(rescheduleResult.Schedule));
    }

    // Dùng ở: dotnet test (phiên xóa không còn trong kết quả, phiên mới được xếp và không tính D).
    [Fact]
    public void AddedAndDeletedSessionsAreHandledWithoutCountingThemAsChanges()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var baseSchedule = new Schedule();
        baseSchedule.Place(1, 0, 1);
        baseSchedule.Place(99, 1, 1);
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule, new HashSet<int> { 99 });
        Assert.False(rescheduleResult.Schedule.IsPlaced(99));
        Assert.True(rescheduleResult.Schedule.IsPlaced(2));
        Assert.Empty(rescheduleResult.ChangedSessionIds);
        Assert.Empty(rescheduleResult.DisplacedSessions);
        Assert.True(baseSchedule.IsPlaced(99));
    }

    // Dùng ở: dotnet test (phòng cũ mất được báo H4; ghim mất phòng không ngăn xếp lại sang phòng khác).
    [Fact]
    public void RemovedRoomDisplacesAndUnlocksItsSession()
    {
        var problem = TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(2, 100)], [TestData.Day(540, 600)], slotMinutes: 60);
        var baseSchedule = new Schedule();
        baseSchedule.Place(1, 0, 1);
        var rescheduleResult = new Rescheduler(problem).Run(baseSchedule, new HashSet<int> { 1 });
        Assert.Equal([ConstraintCode.H4], Assert.Single(rescheduleResult.DisplacedSessions).Codes);
        Assert.True(rescheduleResult.Schedule.TryGetPlacement(1, out var placement));
        Assert.Equal(new Placement(0, 2), placement);
        Assert.Equal([1], rescheduleResult.ChangedSessionIds);
        Assert.Empty(rescheduleResult.UnplacedSessions);
    }

    // ---------- BA QUY MÔ VÀ HAI KỊCH BẢN ----------
    // Dùng ở: dotnet test (18 biến động: hợp lệ, đủ trạng thái, tất định, ít xáo trộn hơn chạy lại M3).
    [Theory]
    [InlineData(30, 1)]
    [InlineData(30, 2)]
    [InlineData(30, 3)]
    [InlineData(60, 1)]
    [InlineData(60, 2)]
    [InlineData(60, 3)]
    [InlineData(100, 1)]
    [InlineData(100, 2)]
    [InlineData(100, 3)]
    public void GeneratedChangesAreValidDeterministicAndPreserveLocks(int sessionCount, int seed)
    {
        var options = sessionCount switch
        {
            30 => GeneratorOptions.Small,
            60 => GeneratorOptions.Medium,
            _ => GeneratorOptions.Large,
        };
        var data = ProblemGenerator.Generate(options with { Seed = seed });
        var baseSchedule = new LocalSearchScheduler(data.Problem).Run().Schedule;
        var originalSchedule = baseSchedule.Clone();
        var smallestRoom = data.Problem.Rooms[0];
        foreach (var room in data.Problem.Rooms)
        {
            if (room.Capacity < smallestRoom.Capacity)
            {
                smallestRoom = room;
            }
        }

        SchedulingProblem[] changedProblems = [
            ChangeScenario.RemoveRoom(data, smallestRoom.Id),
            ChangeScenario.MakeSpeakerUnavailable(data, data.Problem.Sessions[0].SpeakerIds[0], 0),
        ];
        foreach (var changedProblem in changedProblems)
        {
            var rerunSchedule = new LocalSearchScheduler(changedProblem).Run().Schedule;
            var disruption = new DisruptionPenalty(changedProblem, baseSchedule, 0.5);
            var rerunDisruption = disruption.CountDisrupted(rerunSchedule);
            var lockedIds = FindFirstValidLocks(changedProblem, baseSchedule);
            Assert.Equal(3, lockedIds.Count);
            HashSet<int>[] lockSets = [new(), lockedIds];
            foreach (var locks in lockSets)
            {
                var scheduler = new Rescheduler(changedProblem);
                var firstRun = scheduler.Run(baseSchedule, locks);
                var secondRun = scheduler.Run(baseSchedule, locks);
                AssertCompleteAndValid(changedProblem, firstRun);
                LocalSearchTests.AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
                Assert.Equal(firstRun.Schedule.PlacedSessionIds, secondRun.Schedule.PlacedSessionIds);
                Assert.Equal(firstRun.ChangedSessionIds, secondRun.ChangedSessionIds);
                Assert.Equal(firstRun.Value, secondRun.Value);
                Assert.Equal(firstRun.UnplacedSessions.Count, secondRun.UnplacedSessions.Count);
                for (var index = 0; index < firstRun.UnplacedSessions.Count; index++)
                {
                    Assert.Equal(firstRun.UnplacedSessions[index].SessionId, secondRun.UnplacedSessions[index].SessionId);
                    Assert.Equal(firstRun.UnplacedSessions[index].Reason, secondRun.UnplacedSessions[index].Reason);
                }

                Assert.Equal(firstRun.DisplacedSessions.Count, secondRun.DisplacedSessions.Count);
                for (var index = 0; index < firstRun.DisplacedSessions.Count; index++)
                {
                    Assert.Equal(firstRun.DisplacedSessions[index].SessionId, secondRun.DisplacedSessions[index].SessionId);
                    Assert.Equal(firstRun.DisplacedSessions[index].Codes, secondRun.DisplacedSessions[index].Codes);
                }

                Assert.Equal(disruption.CountDisrupted(firstRun.Schedule), firstRun.ChangedSessionIds.Count);
                Assert.Equal(new ObjectiveCalculator(changedProblem, ObjectiveWeights.Default, disruption).Evaluate(firstRun.Schedule).Value, firstRun.Value);
                Assert.True(
                    firstRun.ChangedSessionIds.Count <= rerunDisruption,
                    $"{sessionCount} phiên, seed {seed}, ghim {locks.Count}: D xếp lại = {firstRun.ChangedSessionIds.Count}, D M3 = {rerunDisruption}.");
                foreach (var sessionId in locks)
                {
                    baseSchedule.TryGetPlacement(sessionId, out var basePlacement);
                    Assert.True(firstRun.Schedule.TryGetPlacement(sessionId, out var placement));
                    Assert.Equal(basePlacement, placement);
                }
            }
        }

        LocalSearchTests.AssertSameSchedule(originalSchedule, baseSchedule);
    }

    // ---------- DỰNG DỮ LIỆU VÀ KIỂM BẤT BIẾN ----------
    // Helper của GeneratedChangesAreValidDeterministicAndPreserveLocks (cùng lớp): ba phiên đầu còn hợp lệ ở vị trí cũ.
    private static HashSet<int> FindFirstValidLocks(SchedulingProblem problem, Schedule baseSchedule)
    {
        var checker = new ConstraintChecker(problem);
        var roomIds = new HashSet<int>();
        foreach (var room in problem.Rooms)
        {
            roomIds.Add(room.Id);
        }

        var lockedIds = new HashSet<int>();
        foreach (var session in problem.Sessions)
        {
            if (baseSchedule.TryGetPlacement(session.Id, out var placement)
                && roomIds.Contains(placement.RoomId!.Value)
                && checker.CheckPlacement(new Schedule(), session.Id, placement.SlotIndex, placement.RoomId).Count == 0)
            {
                lockedIds.Add(session.Id);
                if (lockedIds.Count == 3)
                {
                    break;
                }
            }
        }

        return lockedIds;
    }

    // Helper của GeneratedChangesAreValidDeterministicAndPreserveLocks (cùng lớp): mỗi phiên đúng một trạng thái, lịch không vi phạm.
    private static void AssertCompleteAndValid(SchedulingProblem problem, RescheduleResult rescheduleResult)
    {
        Assert.Empty(new ConstraintChecker(problem).Validate(rescheduleResult.Schedule));
        var accountedIds = new HashSet<int>();
        foreach (var sessionId in rescheduleResult.Schedule.PlacedSessionIds)
        {
            Assert.True(accountedIds.Add(sessionId));
            rescheduleResult.Schedule.TryGetPlacement(sessionId, out var placement);
            Assert.NotNull(placement.RoomId);
        }

        foreach (var unplacedSession in rescheduleResult.UnplacedSessions)
        {
            Assert.True(accountedIds.Add(unplacedSession.SessionId));
            Assert.NotEmpty(unplacedSession.BlockedSlotCounts);
            Assert.False(string.IsNullOrWhiteSpace(unplacedSession.Reason));
        }

        Assert.Equal(problem.Sessions.Count, accountedIds.Count);
        foreach (var session in problem.Sessions)
        {
            Assert.Contains(session.Id, accountedIds);
        }
    }

    // Helper của các test ví dụ (cùng lớp): giữ nguyên ví dụ 6.5, Hoa chỉ còn rảnh 10h–11h.
    private static SchedulingProblem CreateChangedExample()
    {
        var originalProblem = ExampleProblem.Create();
        var interests = new List<InterestEntry>();
        for (var attendeeId = 1; attendeeId <= 40; attendeeId++)
        {
            interests.Add(new InterestEntry(attendeeId, ExampleProblem.SessionA));
            var otherSessionId = attendeeId <= 35 ? ExampleProblem.SessionC : ExampleProblem.SessionB;
            interests.Add(new InterestEntry(attendeeId, otherSessionId));
        }

        return new SchedulingProblem(
            originalProblem.Sessions,
            originalProblem.Rooms,
            originalProblem.Days,
            [new SpeakerAvailability(2, 0, 600, 660)],
            interests,
            originalProblem.SlotMinutes);
    }
}
