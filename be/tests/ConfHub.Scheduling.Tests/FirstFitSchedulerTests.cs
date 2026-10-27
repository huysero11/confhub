using ConfHub.Scheduling.Baselines;
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm M1 giữ thứ tự nhập và chọn vị trí đầu tiên, kèm lý do khi chưa xếp được.
public class FirstFitSchedulerTests
{
    // ---------- VÍ DỤ VÀ LÝ DO ----------
    // Dùng ở: dotnet test / Debug Test (đối chiếu kết quả M1 Claude tính tay).
    [Fact]
    public void ExampleMatchesFirstFitPlacementsAndConflict5()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new FirstFitScheduler(problem).Run();
        AssertPlacement(schedulingResult.Schedule, ExampleProblem.SessionA, 0, ExampleProblem.RoomP101);
        AssertPlacement(schedulingResult.Schedule, ExampleProblem.SessionD, 1, ExampleProblem.RoomP101);
        AssertPlacement(schedulingResult.Schedule, ExampleProblem.SessionB, 0, ExampleProblem.RoomP102);
        AssertPlacement(schedulingResult.Schedule, ExampleProblem.SessionC, 1, ExampleProblem.RoomP102);
        Assert.Empty(schedulingResult.UnplacedSessions);
        AssertCompleteAndValid(problem, schedulingResult);
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule);
        Assert.Equal(5, objective.ConflictCount);
        Assert.Equal(164, objective.WastedSeats);
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), objective.Value, 10);
    }

    // Dùng ở: dotnet test (phòng đầu trong dữ liệu được chọn dù lãng phí hơn phòng sau).
    [Fact]
    public void InputSessionAndRoomOrderArePreserved()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(9, 60, 20), TestData.SessionWithSize(2, 60, 20)],
            [TestData.Room(8, 100), TestData.Room(1, 20)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new FirstFitScheduler(problem).Run();
        Assert.Equal([9, 2], schedulingResult.Schedule.PlacedSessionIds);
        AssertPlacement(schedulingResult.Schedule, 9, 0, 8);
        AssertPlacement(schedulingResult.Schedule, 2, 0, 1);
    }

    // Dùng ở: dotnet test (mỗi loại bế tắc có mã và số mốc đúng).
    [Fact]
    public void UnplacedSessionsReportH5H4AndH1()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10), TestData.SessionWithSize(2, 60, 101), TestData.Session(3, 60, 20, 30), TestData.Session(4, 60, 20, 30)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 600)],
            [new SpeakerAvailability(10, 0, 600, 660)],
            slotMinutes: 60);
        var schedulingResult = new FirstFitScheduler(problem).Run();
        Assert.Equal(3, schedulingResult.UnplacedSessions.Count);
        Assert.Equal(1, schedulingResult.UnplacedSessions[0].BlockedSlotCounts[ConstraintCode.H5]);
        Assert.Equal(1, schedulingResult.UnplacedSessions[1].BlockedSlotCounts[ConstraintCode.H4]);
        Assert.Equal(1, schedulingResult.UnplacedSessions[2].BlockedSlotCounts[ConstraintCode.H1]);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (M1 không tự đổi phòng các phiên trước để cứu phiên sau).
    [Fact]
    public void FullRoomReportsH2WithoutMovingEarlierSessions()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 120), TestData.Session(2, 120)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new FirstFitScheduler(problem).Run();
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Equal(2, unplacedSession.SessionId);
        Assert.Equal(1, unplacedSession.BlockedSlotCounts[ConstraintCode.H2]);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (ba quy mô seed 1–5; không yêu cầu heuristic xếp được mọi phiên).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsAreValidCompleteAndDeterministic(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var options in sizes)
        {
            var problem = ProblemGenerator.Generate(options with { Seed = seed }).Problem;
            var scheduler = new FirstFitScheduler(problem);
            var firstRun = scheduler.Run();
            var secondRun = scheduler.Run();
            AssertCompleteAndValid(problem, firstRun);
            AssertCompleteAndValid(problem, secondRun);
            Assert.Equal(firstRun.Schedule.PlacedSessionIds, secondRun.Schedule.PlacedSessionIds);
            foreach (var sessionId in firstRun.Schedule.PlacedSessionIds)
            {
                Assert.True(firstRun.Schedule.TryGetPlacement(sessionId, out var firstPlacement));
                Assert.True(secondRun.Schedule.TryGetPlacement(sessionId, out var secondPlacement));
                Assert.Equal(firstPlacement, secondPlacement);
            }
        }
    }

    // ---------- HÀM KIỂM CHUNG TRONG LỚP ----------
    // Helper của ExampleMatchesFirstFitPlacementsAndConflict5, InputSessionAndRoomOrderArePreserved (cùng lớp).
    private static void AssertPlacement(Schedule schedule, int sessionId, int slotIndex, int roomId)
    {
        Assert.True(schedule.TryGetPlacement(sessionId, out var placement));
        Assert.Equal(new Placement(slotIndex, roomId), placement);
    }

    // Helper của các test (cùng lớp): kiểm cả lịch và phân hoạch mọi phiên thành đã xếp / chưa xếp.
    private static void AssertCompleteAndValid(SchedulingProblem problem, SchedulingResult schedulingResult)
    {
        Assert.Empty(new ConstraintChecker(problem).Validate(schedulingResult.Schedule));
        var accountedSessionIds = new HashSet<int>();
        foreach (var sessionId in schedulingResult.Schedule.PlacedSessionIds)
        {
            Assert.True(accountedSessionIds.Add(sessionId));
            Assert.True(schedulingResult.Schedule.TryGetPlacement(sessionId, out var placement));
            Assert.NotNull(placement.RoomId);
        }

        foreach (var unplacedSession in schedulingResult.UnplacedSessions)
        {
            Assert.True(accountedSessionIds.Add(unplacedSession.SessionId));
            Assert.NotEmpty(unplacedSession.BlockedSlotCounts);
            Assert.False(string.IsNullOrWhiteSpace(unplacedSession.Reason));
        }

        Assert.Equal(problem.Sessions.Count, accountedSessionIds.Count);
        foreach (var session in problem.Sessions)
        {
            Assert.Contains(session.Id, accountedSessionIds);
        }
    }
}
