using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm phương pháp M3 từ điểm vào Run đến lịch và danh sách chưa xếp.
public class LocalSearchSchedulerTests
{
    // ---------- VÍ DỤ VÀ PHIÊN CHƯA XẾP ----------
    // Dùng ở: dotnet test / Debug Test (M3 đi đúng ví dụ 6.5).
    [Fact]
    public void ExampleReturnsConflictFive()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new LocalSearchScheduler(problem).Run();
        AssertCompleteAndValid(problem, schedulingResult);
        Assert.Empty(schedulingResult.UnplacedSessions);
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), schedulingResult.Schedule);
        Assert.Equal(5, new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule).ConflictCount);
    }

    // Dùng ở: dotnet test (không thêm phiên đang thiếu; giữ lý do H4 của tham lam).
    [Fact]
    public void UnplaceableSessionKeepsGreedyReason()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 101), TestData.SessionWithSize(2, 60, 20)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new LocalSearchScheduler(problem).Run();
        var greedyResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        AssertCompleteAndValid(problem, schedulingResult);
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Equal(greedyResult.UnplacedSessions[0].SessionId, unplacedSession.SessionId);
        Assert.Equal(greedyResult.UnplacedSessions[0].Reason, unplacedSession.Reason);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (mỗi phiên đúng một trạng thái, cùng Seed tất định và f không tăng).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsAreCompleteValidAndDeterministic(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var generatorOptions in sizes)
        {
            var problem = ProblemGenerator.Generate(generatorOptions with { Seed = seed }).Problem;
            var scheduler = new LocalSearchScheduler(problem, new ImprovementOptions { Seed = seed });
            var firstRun = scheduler.Run();
            var secondRun = scheduler.Run();
            AssertCompleteAndValid(problem, firstRun);
            LocalSearchTests.AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
            Assert.Equal(firstRun.Schedule.PlacedSessionIds, secondRun.Schedule.PlacedSessionIds);
            var greedyResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            Assert.Equal(greedyResult.Schedule.PlacedSessionIds.Count, firstRun.Schedule.PlacedSessionIds.Count);
            Assert.True(calculator.Evaluate(firstRun.Schedule).Value <= calculator.Evaluate(greedyResult.Schedule).Value + 1e-12);
            Assert.Equal(greedyResult.UnplacedSessions.Count, firstRun.UnplacedSessions.Count);
            for (var index = 0; index < greedyResult.UnplacedSessions.Count; index++)
            {
                Assert.Equal(greedyResult.UnplacedSessions[index].SessionId, firstRun.UnplacedSessions[index].SessionId);
                Assert.Equal(greedyResult.UnplacedSessions[index].Reason, firstRun.UnplacedSessions[index].Reason);
            }
        }
    }

    // Dùng ở: dotnet test (tiêu chí tổng C của M3 trên Medium seed 1–5, không thay bằng so sánh f).
    [Fact]
    public void MediumTotalConflictDoesNotExceedGreedy()
    {
        var totalGreedyConflict = 0;
        var totalLocalConflict = 0;
        for (var seed = 1; seed <= 5; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Medium with { Seed = seed }).Problem;
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            totalGreedyConflict += calculator.Evaluate(new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule).ConflictCount;
            totalLocalConflict += calculator.Evaluate(new LocalSearchScheduler(problem, new ImprovementOptions { Seed = seed }).Run().Schedule).ConflictCount;
        }

        Assert.True(totalLocalConflict <= totalGreedyConflict, $"C M3 = {totalLocalConflict}; C tham lam = {totalGreedyConflict}");
    }

    // ---------- KIỂM KẾT QUẢ DÙNG CHUNG ----------
    // Dùng ở: các test Methods (mỗi phiên có đúng một trạng thái và mọi phiên đã đặt có phòng hợp lệ).
    internal static void AssertCompleteAndValid(SchedulingProblem problem, SchedulingResult schedulingResult)
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
