using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm điểm vào M4, danh sách chưa xếp và tiêu chí tổng C trên Medium seed 1–5.
public class AnnealingSchedulerTests
{
    // ---------- VÍ DỤ VÀ PHIÊN CHƯA XẾP ----------
    // Dùng ở: dotnet test (M4 trên ví dụ 6.5 đạt C = 5).
    [Fact]
    public void ExampleReturnsConflictFive()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new AnnealingScheduler(problem).Run();
        LocalSearchSchedulerTests.AssertCompleteAndValid(problem, schedulingResult);
        Assert.Empty(schedulingResult.UnplacedSessions);
        Assert.Equal(5, new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule).ConflictCount);
    }

    // Dùng ở: dotnet test (M4 không thử xếp phiên quá lớn trong bước cải thiện).
    [Fact]
    public void UnplaceableSessionKeepsGreedyReason()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 101), TestData.SessionWithSize(2, 60, 20)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new AnnealingScheduler(problem).Run();
        var greedyResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        LocalSearchSchedulerTests.AssertCompleteAndValid(problem, schedulingResult);
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Equal(greedyResult.UnplacedSessions[0].SessionId, unplacedSession.SessionId);
        Assert.Equal(greedyResult.UnplacedSessions[0].Reason, unplacedSession.Reason);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (M4 bảo toàn trạng thái từng phiên và tất định kể cả dùng lại scheduler).
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
            var scheduler = new AnnealingScheduler(problem, new ImprovementOptions { Seed = seed });
            var firstRun = scheduler.Run();
            var secondRun = scheduler.Run();
            LocalSearchSchedulerTests.AssertCompleteAndValid(problem, firstRun);
            LocalSearchTests.AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
            Assert.Equal(firstRun.Schedule.PlacedSessionIds, secondRun.Schedule.PlacedSessionIds);
            var greedyResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
            Assert.Equal(greedyResult.Schedule.PlacedSessionIds.Count, firstRun.Schedule.PlacedSessionIds.Count);
            Assert.Equal(greedyResult.UnplacedSessions.Count, firstRun.UnplacedSessions.Count);
            for (var index = 0; index < greedyResult.UnplacedSessions.Count; index++)
            {
                Assert.Equal(greedyResult.UnplacedSessions[index].SessionId, firstRun.UnplacedSessions[index].SessionId);
                Assert.Equal(greedyResult.UnplacedSessions[index].Reason, firstRun.UnplacedSessions[index].Reason);
            }
        }
    }

    // Dùng ở: dotnet test (tiêu chí tổng C của M4 trên Medium seed 1–5, không thay bằng so sánh f).
    [Fact]
    public void MediumTotalConflictDoesNotExceedGreedy()
    {
        var totalGreedyConflict = 0;
        var totalAnnealingConflict = 0;
        for (var seed = 1; seed <= 5; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Medium with { Seed = seed }).Problem;
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            totalGreedyConflict += calculator.Evaluate(new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule).ConflictCount;
            totalAnnealingConflict += calculator.Evaluate(new AnnealingScheduler(problem, new ImprovementOptions { Seed = seed }).Run().Schedule).ConflictCount;
        }

        Assert.True(totalAnnealingConflict <= totalGreedyConflict, $"C M4 = {totalAnnealingConflict}; C tham lam = {totalGreedyConflict}");
    }
}
