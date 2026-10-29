using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm M3R ưu tiên số phiên trước f và tái lập mọi lần khởi động bằng Seed.
public class RestartSchedulerTests
{
    // ---------- VÍ DỤ VÀ SỐ LẦN KHỞI ĐỘNG ----------
    // Dùng ở: dotnet test (M3R trên ví dụ 6.5 vẫn đạt C = 5).
    [Fact]
    public void ExampleReturnsConflictFive()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new RestartScheduler(problem).Run();
        LocalSearchSchedulerTests.AssertCompleteAndValid(problem, schedulingResult);
        Assert.Empty(schedulingResult.UnplacedSessions);
        Assert.Equal(5, new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule).ConflictCount);
    }

    // Dùng ở: dotnet test (lần đầu dùng đúng thứ tự ràng buộc; RestartCount = 1 phải trùng M3).
    [Fact]
    public void SingleRestartMatchesLocalSearchScheduler()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Small).Problem;
        var options = new ImprovementOptions { RestartCount = 1 };
        var restartResult = new RestartScheduler(problem, options).Run();
        var localResult = new LocalSearchScheduler(problem, options).Run();
        LocalSearchTests.AssertSameSchedule(localResult.Schedule, restartResult.Schedule);
    }

    // Dùng ở: dotnet test (danh sách chưa xếp phải thuộc đúng lần chạy thắng, kể cả khi không phiên nào đặt được).
    [Fact]
    public void CompletelyUnplaceableProblemPreservesAllReasons()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 101), TestData.SessionWithSize(2, 60, 102)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new RestartScheduler(problem).Run();
        LocalSearchSchedulerTests.AssertCompleteAndValid(problem, schedulingResult);
        Assert.Empty(schedulingResult.Schedule.PlacedSessionIds);
        Assert.Equal(2, schedulingResult.UnplacedSessions.Count);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Dùng ở: dotnet test (M3R không mất phiên so với M3; cùng số phiên thì f không tệ hơn; cùng Seed tất định).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsAreCompleteDeterministicAndNotWorseThanM3(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var generatorOptions in sizes)
        {
            var problem = ProblemGenerator.Generate(generatorOptions with { Seed = seed }).Problem;
            var options = new ImprovementOptions { Seed = seed };
            var scheduler = new RestartScheduler(problem, options);
            var firstRun = scheduler.Run();
            var secondRun = scheduler.Run();
            var localResult = new LocalSearchScheduler(problem, options).Run();
            LocalSearchSchedulerTests.AssertCompleteAndValid(problem, firstRun);
            LocalSearchTests.AssertSameSchedule(firstRun.Schedule, secondRun.Schedule);
            Assert.Equal(firstRun.Schedule.PlacedSessionIds, secondRun.Schedule.PlacedSessionIds);
            Assert.True(firstRun.Schedule.PlacedSessionIds.Count >= localResult.Schedule.PlacedSessionIds.Count);
            if (firstRun.Schedule.PlacedSessionIds.Count == localResult.Schedule.PlacedSessionIds.Count)
            {
                var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
                Assert.True(calculator.Evaluate(firstRun.Schedule).Value <= calculator.Evaluate(localResult.Schedule).Value + 1e-12);
            }

            Assert.Equal(firstRun.UnplacedSessions.Count, secondRun.UnplacedSessions.Count);
            for (var index = 0; index < firstRun.UnplacedSessions.Count; index++)
            {
                Assert.Equal(firstRun.UnplacedSessions[index].SessionId, secondRun.UnplacedSessions[index].SessionId);
                Assert.Equal(firstRun.UnplacedSessions[index].Reason, secondRun.UnplacedSessions[index].Reason);
            }
        }
    }
}
