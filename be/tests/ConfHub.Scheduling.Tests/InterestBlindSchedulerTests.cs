using ConfHub.Scheduling.Baselines;
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm M2 bỏ trọng số C, vẫn gán đủ phòng và được chấm bằng trọng số mặc định.
public class InterestBlindSchedulerTests
{
    // ---------- VÍ DỤ VÀ TRỌNG SỐ ----------
    // Dùng ở: dotnet test / Debug Test (đối chiếu kết quả M2 Claude tính tay).
    [Fact]
    public void ExampleMatchesInterestBlindPlacementsAndConflict5()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new InterestBlindScheduler(problem).Run();
        var expectedSchedule = ExampleProblem.CreateSwappedSchedule();
        foreach (var sessionId in expectedSchedule.PlacedSessionIds)
        {
            Assert.True(expectedSchedule.TryGetPlacement(sessionId, out var expectedPlacement));
            Assert.True(schedulingResult.Schedule.TryGetPlacement(sessionId, out var actualPlacement));
            Assert.Equal(expectedPlacement, actualPlacement);
        }

        Assert.Empty(schedulingResult.UnplacedSessions);
        AssertCompleteAndValid(problem, schedulingResult);
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule);
        Assert.Equal(5, objective.ConflictCount);
        Assert.Equal(164, objective.WastedSeats);
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), objective.Value, 10);
    }

    // Dùng ở: dotnet test (C bị bỏ, B : S giữ tỉ lệ 2 : 1).
    [Fact]
    public void WeightsIgnoreConflictAndPreserveLoadToWasteRatio()
    {
        Assert.Equal(0.0, InterestBlindScheduler.Weights.Conflict);
        Assert.Equal(2.0 / 3.0, InterestBlindScheduler.Weights.Load, 12);
        Assert.Equal(1.0 / 3.0, InterestBlindScheduler.Weights.Waste, 12);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (ba quy mô seed 1–5; kết quả hợp lệ, đủ trạng thái và tất định).
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
            var scheduler = new InterestBlindScheduler(problem);
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

    // Helper của các test (cùng lớp): mọi phiên phải có đúng một trạng thái, đã xếp thì có phòng hợp lệ.
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
