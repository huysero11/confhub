using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm leo đồi theo từng vòng của ví dụ 6.5 và bất biến trên ba quy mô dữ liệu sinh.
public class LocalSearchTests
{
    // ---------- VÍ DỤ VÀ ĐIỀU KIỆN DỪNG ----------
    // Dùng ở: dotnet test / Debug Test (vòng 1 chỉ nhận C ↔ B; vòng 2 dừng ở C = 5).
    [Fact]
    public void ExampleAcceptsOnlySwapCBThenStopsOnSecondIteration()
    {
        var problem = ExampleProblem.Create();
        var start = new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule;
        var originalSchedule = start.Clone();
        var improvementResult = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions()).Improve(start);
        Assert.Equal(1, improvementResult.AcceptedMoves);
        Assert.Equal(2, improvementResult.Iterations);
        AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), improvementResult.Schedule);
        AssertSameSchedule(originalSchedule, start);
        Assert.Empty(new ConstraintChecker(problem).Validate(improvementResult.Schedule));
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(improvementResult.Schedule);
        Assert.Equal(5, objective.ConflictCount);
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), improvementResult.Value, 12);
    }

    // Dùng ở: dotnet test (giới hạn vòng chính xác; lịch rỗng không mất phiên hoặc sinh bước giả).
    [Fact]
    public void IterationLimitAndEmptyScheduleAreHandled()
    {
        var problem = ExampleProblem.Create();
        var search = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions { MaxIterations = 1 });
        var limitedResult = search.Improve(ExampleProblem.CreateGreedySchedule());
        Assert.Equal(1, limitedResult.Iterations);
        Assert.Equal(1, limitedResult.AcceptedMoves);
        AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), limitedResult.Schedule);

        var emptyResult = search.Improve(new Schedule());
        Assert.Empty(emptyResult.Schedule.PlacedSessionIds);
        Assert.Equal(0, emptyResult.AcceptedMoves);
        Assert.Equal(1, emptyResult.Iterations);
        Assert.Equal(0, emptyResult.Value);
    }

    // Dùng ở: dotnet test (Q1 cố ý dừng khi C = 0, dù thành phần tải còn có thể giảm).
    [Fact]
    public void ZeroContributionsDoNotCreateCandidatesForLoadOnlyImprovement()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var start = new Schedule();
        start.Place(1, 0, 1);
        start.Place(2, 0, 2);
        var improvementResult = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions()).Improve(start);
        Assert.Equal(0, improvementResult.AcceptedMoves);
        AssertSameSchedule(start, improvementResult.Schedule);
    }

    // Dùng ở: dotnet test (lịch đầu vi phạm hoặc thiếu phòng phải báo lỗi trước khi tìm kiếm).
    [Fact]
    public void InvalidOrRoomlessStartIsRejected()
    {
        var search = new LocalSearch(ExampleProblem.Create(), ObjectiveWeights.Default, new ImprovementOptions());
        var start = ExampleProblem.CreateGreedySchedule();
        start.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP102);
        Assert.Throws<ArgumentException>(() => search.Improve(start));
        start = ExampleProblem.CreateGreedySchedule();
        start.SetRoom(ExampleProblem.SessionA, null);
        Assert.Throws<ArgumentException>(() => search.Improve(start));
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (lịch hợp lệ, không mất phiên, f không tăng, lặp lại cho cùng kết quả).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsPreserveValidityScoreSessionsAndDeterminism(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var generatorOptions in sizes)
        {
            var problem = ProblemGenerator.Generate(generatorOptions with { Seed = seed }).Problem;
            var start = new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule;
            var originalSchedule = start.Clone();
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            var search = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions { Seed = seed });
            var firstRun = search.Improve(start);
            var secondRun = search.Improve(start);
            Assert.Empty(new ConstraintChecker(problem).Validate(firstRun.Schedule));
            Assert.True(firstRun.Value <= calculator.Evaluate(start).Value + 1e-12);
            Assert.Equal(calculator.Evaluate(firstRun.Schedule).Value, firstRun.Value, 12);
            Assert.Equal(start.PlacedSessionIds.Count, firstRun.Schedule.PlacedSessionIds.Count);
            foreach (var sessionId in start.PlacedSessionIds)
            {
                Assert.True(firstRun.Schedule.IsPlaced(sessionId));
            }

            AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
            Assert.Equal(firstRun.Iterations, secondRun.Iterations);
            Assert.Equal(firstRun.AcceptedMoves, secondRun.AcceptedMoves);
            AssertSameSchedule(originalSchedule, start);
        }
    }

    // Dùng ở: dotnet test (chạy từng vòng độc lập để kiểm mọi bước nhận đều giảm f quá sai số).
    [Fact]
    public void EveryAcceptedStepStrictlyDecreasesObjective()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Small).Problem;
        var schedule = new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule;
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
        var singleIteration = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions { MaxIterations = 1 });
        var acceptedMoves = 0;
        for (var iteration = 0; iteration < 200; iteration++)
        {
            var previousScore = calculator.Evaluate(schedule).Value;
            var improvementResult = singleIteration.Improve(schedule);
            if (improvementResult.AcceptedMoves == 0)
            {
                Assert.True(acceptedMoves > 0);
                var fullResult = new LocalSearch(problem, ObjectiveWeights.Default, new ImprovementOptions()).Improve(new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule);
                AssertSameSchedule(fullResult.Schedule, schedule);
                Assert.Equal(acceptedMoves, fullResult.AcceptedMoves);
                return;
            }

            Assert.Equal(1, improvementResult.AcceptedMoves);
            Assert.True(improvementResult.Value < previousScore - 1e-12);
            Assert.Empty(new ConstraintChecker(problem).Validate(improvementResult.Schedule));
            schedule = improvementResult.Schedule;
            acceptedMoves++;
        }

        Assert.Fail("Dữ liệu Small phải hội tụ trong 200 vòng.");
    }

    // ---------- SO SÁNH LỊCH DÙNG CHUNG ----------
    // Dùng ở: các test Improvement/Methods (so từng vị trí, độc lập với thứ tự lưu trong Schedule).
    internal static void AssertSameSchedule(Schedule expectedSchedule, Schedule actualSchedule)
    {
        Assert.Equal(expectedSchedule.PlacedSessionIds.Count, actualSchedule.PlacedSessionIds.Count);
        foreach (var sessionId in expectedSchedule.PlacedSessionIds)
        {
            Assert.True(expectedSchedule.TryGetPlacement(sessionId, out var expectedPlacement));
            Assert.True(actualSchedule.TryGetPlacement(sessionId, out var actualPlacement));
            Assert.Equal(expectedPlacement, actualPlacement);
        }
    }
}
