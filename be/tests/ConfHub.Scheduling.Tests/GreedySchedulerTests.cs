using ConfHub.Scheduling.Baselines;
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm tham lam theo bảng 6.5, lý do thất bại và các bất biến trên dữ liệu sinh.
public class GreedySchedulerTests
{
    // ---------- VÍ DỤ VÀ THỨ TỰ ----------
    // Dùng ở: dotnet test / Debug Test (đi từng bước A, D, B, C của ví dụ 6.5).
    [Fact]
    public void ExampleMatchesEveryPlacementAndObjective()
    {
        var problem = ExampleProblem.Create();
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        var expectedSchedule = ExampleProblem.CreateGreedySchedule();
        foreach (var sessionId in expectedSchedule.PlacedSessionIds)
        {
            Assert.True(expectedSchedule.TryGetPlacement(sessionId, out var expectedPlacement));
            Assert.True(schedulingResult.Schedule.TryGetPlacement(sessionId, out var actualPlacement));
            Assert.Equal(expectedPlacement, actualPlacement);
        }

        Assert.Empty(schedulingResult.UnplacedSessions);
        AssertCompleteAndValid(problem, schedulingResult);
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedulingResult.Schedule);
        Assert.Equal(35, objective.ConflictCount);
        Assert.Equal(164, objective.WastedSeats);
        Assert.Equal(0.0, objective.LoadStdDev, 10);
        Assert.Equal((0.7 * 35.0 / 40.0) + (0.1 * 164.0 / 284.0), objective.Value, 10);
    }

    // Dùng ở: dotnet test (thứ tự khởi động lại của T2.3 thực sự được áp dụng).
    [Fact]
    public void CustomOrderProducesAnotherValidSchedule()
    {
        var problem = ExampleProblem.Create();
        var scheduler = new GreedyScheduler(problem, ObjectiveWeights.Default);
        var originalSchedule = scheduler.Run().Schedule;
        var schedulingResult = scheduler.Run([4, 1, 3, 2]);
        AssertCompleteAndValid(problem, schedulingResult);

        // Thứ tự tùy chọn có thể để lại phiên chưa xếp; tính hợp lệ không đòi lịch phải đầy đủ.
        Assert.Equal(ExampleProblem.SessionC, schedulingResult.Schedule.PlacedSessionIds[0]);
        var hasDifferentPlacement = false;
        foreach (var session in problem.Sessions)
        {
            var isOriginallyPlaced = originalSchedule.TryGetPlacement(session.Id, out var originalPlacement);
            var isCustomPlaced = schedulingResult.Schedule.TryGetPlacement(session.Id, out var customPlacement);
            if (isOriginallyPlaced != isCustomPlaced || originalPlacement != customPlacement)
            {
                hasDifferentPlacement = true;
            }
        }

        Assert.True(hasDifferentPlacement);
    }

    // Dùng ở: dotnet test (không bỏ sót, lặp hoặc nhận mã lạ trong sessionOrder).
    [Fact]
    public void InvalidSessionOrdersAreRejected()
    {
        var scheduler = new GreedyScheduler(ExampleProblem.Create(), ObjectiveWeights.Default);
        Assert.Throws<ArgumentException>(() => scheduler.Run([1, 2, 3]));
        Assert.Throws<ArgumentException>(() => scheduler.Run([1, 2, 3, 4, 5]));
        Assert.Throws<ArgumentException>(() => scheduler.Run([1, 2, 3, 3]));
        Assert.Throws<ArgumentException>(() => scheduler.Run([1, 2, 3, 99]));
    }

    // ---------- PHIÊN CHƯA XẾP VÀ PHÁ HÒA ----------
    // Dùng ở: dotnet test (diễn giả khai rảnh ngoài giờ tổ chức thì chỉ báo H5).
    [Fact]
    public void NoAvailableStartReportsOnlyH5()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            [new SpeakerAvailability(10, 0, 720, 780)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Single(unplacedSession.BlockedSlotCounts);
        Assert.Equal(2, unplacedSession.BlockedSlotCounts[ConstraintCode.H5]);
        Assert.Equal("Phiên 1 không có mốc bắt đầu hợp lệ (H5).", unplacedSession.Reason);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (không phòng nào đủ lớn, số mốc H4 phải chính xác).
    [Fact]
    public void OversizedSessionReportsH4AtEveryValidSlot()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 101)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Single(unplacedSession.BlockedSlotCounts);
        Assert.Equal(2, unplacedSession.BlockedSlotCounts[ConstraintCode.H4]);
        Assert.Contains("phòng không đủ sức chứa (H4) ở 2 mốc", unplacedSession.Reason);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (hai diễn giả cùng bận vẫn chỉ là một mốc bị H1 chặn).
    [Fact]
    public void SharedSpeakersCountH1OnlyOncePerSlot()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10, 20), TestData.Session(2, 60, 10, 20)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 660)],
            [new SpeakerAvailability(10, 0, 540, 600), new SpeakerAvailability(20, 0, 540, 600)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Equal(2, unplacedSession.SessionId);
        Assert.Single(unplacedSession.BlockedSlotCounts);
        Assert.Equal(1, unplacedSession.BlockedSlotCounts[ConstraintCode.H1]);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (H2 lặp qua nhiều mốc trong một phiên dài chỉ đếm một lần cho vị trí thử).
    [Fact]
    public void ConcurrencyViolationsCountOncePerCandidateSlot()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 120), TestData.Session(2, 120)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Single(unplacedSession.BlockedSlotCounts);
        Assert.Equal(1, unplacedSession.BlockedSlotCounts[ConstraintCode.H2]);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (không có phòng trống suốt khoảng dù H2 vẫn hợp lệ ở từng mốc).
    [Fact]
    public void FragmentedRoomsReportH2FromAssignment()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 20, 10), TestData.SessionWithSize(2, 60, 80, 20), TestData.SessionWithSize(3, 120, 20)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 660)],
            [new SpeakerAvailability(10, 0, 540, 600), new SpeakerAvailability(20, 0, 600, 660)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run([1, 2, 3]);
        var unplacedSession = Assert.Single(schedulingResult.UnplacedSessions);
        Assert.Equal(3, unplacedSession.SessionId);
        Assert.Equal(1, unplacedSession.BlockedSlotCounts[ConstraintCode.H2]);
        AssertCompleteAndValid(problem, schedulingResult);
    }

    // Dùng ở: dotnet test (khi f hòa, lấy mốc sớm hơn).
    [Fact]
    public void EqualScoresChooseEarliestSlot()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
        Assert.True(schedulingResult.Schedule.TryGetPlacement(1, out var placement));
        Assert.Equal(0, placement.SlotIndex);
    }

    // ---------- DỮ LIỆU SINH VÀ SO SÁNH ----------
    // Dùng ở: dotnet test (15 bài toán; kiểm lịch hợp lệ, đủ trạng thái và tất định cả khi dùng lại scheduler).
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
            var scheduler = new GreedyScheduler(problem, ObjectiveWeights.Default);
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

            Assert.Equal(firstRun.UnplacedSessions.Count, secondRun.UnplacedSessions.Count);
            for (var index = 0; index < firstRun.UnplacedSessions.Count; index++)
            {
                Assert.Equal(firstRun.UnplacedSessions[index].SessionId, secondRun.UnplacedSessions[index].SessionId);
                Assert.Equal(firstRun.UnplacedSessions[index].Reason, secondRun.UnplacedSessions[index].Reason);
            }
        }
    }

    // Dùng ở: dotnet test (phòng theo phân vị giúp tham lam xếp ít nhất 95% phiên ở cả ba quy mô).
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void GeneratedProblemsPlaceAtLeastNinetyFivePercentOfSessions(int seed)
    {
        GeneratorOptions[] sizes = [GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large];
        foreach (var options in sizes)
        {
            var problem = ProblemGenerator.Generate(options with { Seed = seed }).Problem;
            var schedulingResult = new GreedyScheduler(problem, ObjectiveWeights.Default).Run();
            var placedCount = schedulingResult.Schedule.PlacedSessionIds.Count;
            var totalCount = problem.Sessions.Count;

            Assert.True(placedCount * 100 >= totalCount * 95, $"Seed {seed}: xếp được {placedCount}/{totalCount} phiên.");
        }
    }

    // Dùng ở: dotnet test (giữ nguyên tiêu chí tổng C tham lam <= M1 trên Medium seed 1–5).
    [Fact]
    public void MediumTotalGreedyConflictDoesNotExceedFirstFit()
    {
        var totalGreedyConflict = 0;
        var totalFirstFitConflict = 0;
        for (var seed = 1; seed <= 5; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Medium with { Seed = seed }).Problem;
            var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
            totalGreedyConflict += calculator.Evaluate(new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule).ConflictCount;
            totalFirstFitConflict += calculator.Evaluate(new FirstFitScheduler(problem).Run().Schedule).ConflictCount;
        }

        Assert.True(totalGreedyConflict <= totalFirstFitConflict, $"C tham lam = {totalGreedyConflict}; C M1 = {totalFirstFitConflict}");
    }

    // Helper của các test (cùng lớp): mỗi phiên đúng một trạng thái, phiên đã xếp luôn có phòng và không vi phạm.
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
