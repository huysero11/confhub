using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm lịch nhiệt, bảo toàn lịch tốt nhất và tính tất định theo Seed của luyện kim.
public class SimulatedAnnealingTests
{
    // ---------- VÍ DỤ VÀ LỊCH NHIỆT ----------
    // Dùng ở: dotnet test / Debug Test (ví dụ 6.5 đạt C = 5 và không sửa lịch đầu).
    [Fact]
    public void ExampleReachesConflictFiveAndPreservesInput()
    {
        var problem = ExampleProblem.Create();
        var start = ExampleProblem.CreateGreedySchedule();
        var improvementResult = new SimulatedAnnealing(problem, ObjectiveWeights.Default, new ImprovementOptions()).Improve(start);
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(improvementResult.Schedule);
        Assert.Equal(5, objective.ConflictCount);
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), improvementResult.Value, 12);
        Assert.Empty(new ConstraintChecker(problem).Validate(improvementResult.Schedule));
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateGreedySchedule(), start);
    }

    // Dùng ở: dotnet test (phép bất khả thi vẫn đếm bước; cuối lịch nhiệt luôn có vòng cục bộ).
    [Fact]
    public void InvalidAttemptsStillCountAndCoolingStopsAtMinimum()
    {
        var problem = TestData.CreateProblem([TestData.Session(1, 60)], [TestData.Room(1, 100)], [TestData.Day(540, 600)], slotMinutes: 60);
        var start = new Schedule();
        start.Place(1, 0, 1);
        var options = new ImprovementOptions { InitialTemperature = 0.08, MinTemperature = 0.02, CoolingRate = 0.5, StepsPerTemperature = 3 };
        var improvementResult = new SimulatedAnnealing(problem, ObjectiveWeights.Default, options).Improve(start);
        Assert.Equal(10, improvementResult.Iterations); // 3 mức nhiệt × 3 bước + 1 vòng cục bộ.
        Assert.Equal(0, improvementResult.AcceptedMoves);
        LocalSearchTests.AssertSameSchedule(start, improvementResult.Schedule);
    }

    // Dùng ở: dotnet test (luyện kim không dùng Q1 nên có thể dời phiên có đóng góp 0 để giảm tải).
    [Fact]
    public void AnnealingCanImproveLoadWhenEveryConflictContributionIsZero()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var start = new Schedule();
        start.Place(1, 0, 1);
        start.Place(2, 0, 2);
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
        var improvementResult = new SimulatedAnnealing(problem, ObjectiveWeights.Default, new ImprovementOptions()).Improve(start);
        Assert.True(improvementResult.Value < calculator.Evaluate(start).Value);
        Assert.Equal(0, calculator.Evaluate(improvementResult.Schedule).LoadStdDev);
        Assert.Empty(new ConstraintChecker(problem).Validate(improvementResult.Schedule));
    }

    // Dùng ở: dotnet test (lịch rỗng vẫn chạy cục bộ cuối; lịch đầu sai bị loại).
    [Fact]
    public void EmptyAndInvalidStartsAreHandled()
    {
        var annealing = new SimulatedAnnealing(ExampleProblem.Create(), ObjectiveWeights.Default, new ImprovementOptions());
        var emptyResult = annealing.Improve(new Schedule());
        Assert.Empty(emptyResult.Schedule.PlacedSessionIds);
        Assert.Equal(1, emptyResult.Iterations);
        Assert.Equal(0, emptyResult.AcceptedMoves);
        var start = ExampleProblem.CreateGreedySchedule();
        start.SetRoom(1, null);
        Assert.Throws<ArgumentException>(() => annealing.Improve(start));
        start.Place(1, 0, 2);
        Assert.Throws<ArgumentException>(() => annealing.Improve(start));
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (cùng Seed ra cùng lịch; lịch tốt nhất không tệ hơn lịch tham lam đầu vào).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsPreserveBestScoreValiditySessionsAndDeterminism(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var generatorOptions in sizes)
        {
            var problem = ProblemGenerator.Generate(generatorOptions with { Seed = seed }).Problem;
            var start = new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule;
            var originalSchedule = start.Clone();
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            var annealing = new SimulatedAnnealing(problem, ObjectiveWeights.Default, new ImprovementOptions { Seed = seed });
            var firstRun = annealing.Improve(start);
            var secondRun = annealing.Improve(start);
            Assert.Empty(new ConstraintChecker(problem).Validate(firstRun.Schedule));
            Assert.True(firstRun.Value <= calculator.Evaluate(start).Value + 1e-12);
            Assert.Equal(calculator.Evaluate(firstRun.Schedule).Value, firstRun.Value, 12);
            Assert.Equal(start.PlacedSessionIds.Count, firstRun.Schedule.PlacedSessionIds.Count);
            foreach (var sessionId in start.PlacedSessionIds)
            {
                Assert.True(firstRun.Schedule.IsPlaced(sessionId));
            }

            LocalSearchTests.AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
            Assert.Equal(firstRun.Iterations, secondRun.Iterations);
            Assert.Equal(firstRun.AcceptedMoves, secondRun.AcceptedMoves);
            LocalSearchTests.AssertSameSchedule(originalSchedule, start);
        }
    }

    // Dùng ở: dotnet test (Seed khác được phép cho lịch khác, nhưng vẫn phải giữ các bất biến).
    [Theory]
    [InlineData(7)]
    [InlineData(19)]
    public void DifferentSeedsStillReturnValidNonWorseningSchedules(int seed)
    {
        var problem = ExampleProblem.Create();
        var start = ExampleProblem.CreateGreedySchedule();
        var improvementResult = new SimulatedAnnealing(problem, ObjectiveWeights.Default, new ImprovementOptions { Seed = seed }).Improve(start);
        Assert.Empty(new ConstraintChecker(problem).Validate(improvementResult.Schedule));
        Assert.Equal(5, new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(improvementResult.Schedule).ConflictCount);
    }
}
