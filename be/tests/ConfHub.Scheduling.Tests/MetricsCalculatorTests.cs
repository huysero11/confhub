// Kiểm chứng chỉ số bằng ví dụ tính tay, các biên nguyện vọng và dữ liệu sinh.
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class MetricsCalculatorTests
{
    // ---------- VÍ DỤ TÍNH TAY ----------
    // Hai lịch phải khớp nguyên số liệu T2.4, kể cả tổng sức chứa theo từng phiên.
    [Theory]
    [InlineData(false, 0.125, 35)]
    [InlineData(true, 0.875, 5)]
    public void ExampleMatchesHandCalculatedMetrics(bool isSwapped, double noLossRate, int conflictCount)
    {
        var problem = ExampleProblem.Create();
        var schedule = isSwapped ? ExampleProblem.CreateSwappedSchedule() : ExampleProblem.CreateGreedySchedule();
        var metrics = new MetricsCalculator(problem, CreateExampleInterests(), ObjectiveWeights.Default).Calculate(schedule);

        Assert.Equal(4, metrics.SessionCount);
        Assert.Equal(4, metrics.PlacedCount);
        Assert.Equal(1, metrics.PlacementRate);
        Assert.Equal(noLossRate, metrics.NoLossAttendeeRate);
        Assert.Equal(116.0 / 280, metrics.RoomFillRate, 12);
        Assert.Equal(164, metrics.WastedSeats);
        Assert.Equal(conflictCount, metrics.ConflictCount);
        Assert.Equal(0, metrics.LoadStdDev);
        Assert.Equal(0, metrics.ViolationCount);
        Assert.Equal(new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedule).Value, metrics.Objective);
    }

    // Bỏ D không ảnh hưởng nguyện vọng; bỏ thêm C làm tất cả 40 người mất phiên.
    [Fact]
    public void MissingSessionsCountAsLossOnlyForInterestedAttendees()
    {
        var calculator = new MetricsCalculator(ExampleProblem.Create(), CreateExampleInterests(), ObjectiveWeights.Default);
        var schedule = ExampleProblem.CreateSwappedSchedule();
        schedule.Remove(ExampleProblem.SessionD);
        var withoutUnwantedSession = calculator.Calculate(schedule);
        Assert.Equal(35.0 / 40, withoutUnwantedSession.NoLossAttendeeRate);
        Assert.Equal(0.75, withoutUnwantedSession.PlacementRate);

        schedule.Remove(ExampleProblem.SessionC);
        var withoutWantedSession = calculator.Calculate(schedule);
        Assert.Equal(0, withoutWantedSession.NoLossAttendeeRate);
        Assert.Equal(0.5, withoutWantedSession.PlacementRate);
    }

    // ---------- TRƯỜNG HỢP BIÊN ----------
    // Mã người thưa và nguyện vọng trùng không làm đổi mẫu số.
    [Fact]
    public void OnlyDistinctAttendeesWithInterestsEnterDenominator()
    {
        List<InterestEntry> interests =
        [
            new(2, ExampleProblem.SessionA),
            new(2, ExampleProblem.SessionA),
            new(100, ExampleProblem.SessionC),
        ];
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, 0, ExampleProblem.RoomP101);
        var metrics = new MetricsCalculator(ExampleProblem.Create(), interests, ObjectiveWeights.Default).Calculate(schedule);
        Assert.Equal(0.5, metrics.NoLossAttendeeRate);
    }

    // Lịch trống không đáp ứng người có nguyện vọng, không có tỷ lệ lấp đầy.
    [Fact]
    public void EmptyScheduleHasZeroRates()
    {
        var metrics = new MetricsCalculator(ExampleProblem.Create(), CreateExampleInterests(), ObjectiveWeights.Default).Calculate(new Schedule());
        Assert.Equal(0, metrics.PlacementRate);
        Assert.Equal(0, metrics.NoLossAttendeeRate);
        Assert.Equal(0, metrics.RoomFillRate);
        Assert.Equal(0, metrics.ViolationCount);
    }

    // Quy ước tỷ lệ có mẫu số rỗng là 0; phiên chưa có phòng không tạo sức chứa.
    [Fact]
    public void NoInterestsAndNoAssignedRoomsHaveZeroRates()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, 0);
        var metrics = new MetricsCalculator(ExampleProblem.Create(), [], ObjectiveWeights.Default).Calculate(schedule);
        Assert.Equal(0, metrics.NoLossAttendeeRate);
        Assert.Equal(0, metrics.RoomFillRate);
    }

    // Chồng khoảng khác giờ bắt đầu mới gây mất; chạm đầu mút hoặc khác ngày thì không.
    [Theory]
    [InlineData(0, 570, 0)]
    [InlineData(0, 600, 1)]
    [InlineData(1, 540, 1)]
    public void LossUsesIntervalsAndDay(int secondDay, int secondStart, double expectedRate)
    {
        List<InterestEntry> interests = [new(1, 1), new(1, 2)];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 10), TestData.Room(2, 10)],
            [TestData.Day(540, 720), TestData.Day(540, 720)],
            interests: interests,
            slotMinutes: 30);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, TestData.SlotIndexAt(problem, secondDay, secondStart), 2);
        var metrics = new MetricsCalculator(problem, interests, ObjectiveWeights.Default).Calculate(schedule);
        Assert.Equal(expectedRate, metrics.NoLossAttendeeRate);
    }

    // Lịch vi phạm phải được báo thật, không giả định đầu vào luôn hợp lệ.
    [Fact]
    public void InvalidRoomCapacityIsReported()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, 0, ExampleProblem.RoomP102);
        var metrics = new MetricsCalculator(ExampleProblem.Create(), CreateExampleInterests(), ObjectiveWeights.Default).Calculate(schedule);
        Assert.True(metrics.ViolationCount > 0);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Tỷ lệ của lịch hợp lệ trong [0, 1], f dùng đúng bộ trọng số truyền vào.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GeneratedSchedulesHaveBoundedRatesAndMatchingObjective(int seed)
    {
        var data = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = seed });
        var schedule = new LocalSearchScheduler(data.Problem).Run().Schedule;
        var weights = new ObjectiveWeights(0.5, 0.3, 0.2);
        var metrics = new MetricsCalculator(data.Problem, data.Interests, weights).Calculate(schedule);
        Assert.InRange(metrics.PlacementRate, 0, 1);
        Assert.InRange(metrics.NoLossAttendeeRate, 0, 1);
        Assert.InRange(metrics.RoomFillRate, 0, 1);
        Assert.Equal(0, metrics.ViolationCount);
        Assert.Equal(new ObjectiveCalculator(data.Problem, weights).Evaluate(schedule).Value, metrics.Objective);
    }

    // Helper của các test ví dụ (cùng lớp): tạo đúng 40 người theo mục 3.4.
    private static List<InterestEntry> CreateExampleInterests()
    {
        var interests = new List<InterestEntry>();
        for (var attendeeId = 1; attendeeId <= 40; attendeeId++)
        {
            interests.Add(new InterestEntry(attendeeId, ExampleProblem.SessionA));
            interests.Add(new InterestEntry(attendeeId, attendeeId <= 35 ? ExampleProblem.SessionC : ExampleProblem.SessionB));
        }

        return interests;
    }
}
