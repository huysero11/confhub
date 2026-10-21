using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class ObjectiveCalculatorTests
{
    private const int Precision = 6;

    // ---------- VÍ DỤ CHẠY TAY (PROJECT.md 6.5) ----------
    [Fact]
    public void GreedyScheduleOfTheExampleHasConflict35()
    {
        var calculator = new ObjectiveCalculator(ExampleProblem.Create(), ObjectiveWeights.Default);

        var result = calculator.Evaluate(ExampleProblem.CreateGreedySchedule());

        Assert.Equal(35, result.ConflictCount);
        Assert.Equal(0.0, result.LoadStdDev, Precision);
        Assert.Equal(164, result.WastedSeats);
        Assert.Equal(0.875, result.NormalizedConflict, Precision);
        Assert.Equal(164.0 / 284.0, result.NormalizedWaste, Precision);

        // f = 0,7 * 35/40 + 0,2 * 0 + 0,1 * 164/284 = 0,6702465 (làm tròn 6 chữ số thập phân: 0,670246).
        Assert.Equal((0.7 * 0.875) + (0.1 * 164.0 / 284.0), result.Value, Precision);
    }

    [Fact]
    public void SwappedScheduleOfTheExampleHasConflict5()
    {
        var calculator = new ObjectiveCalculator(ExampleProblem.Create(), ObjectiveWeights.Default);

        var result = calculator.Evaluate(ExampleProblem.CreateSwappedSchedule());

        Assert.Equal(5, result.ConflictCount);
        Assert.Equal(164, result.WastedSeats);

        // f = 0,7 * 5/40 + 0,1 * 164/284 = 0,1452465.
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), result.Value, Precision);
    }

    // ---------- B: ĐỘ LỆCH CHUẨN TẢI ----------
    [Fact]
    public void EvenLoadHasZeroStdDev()
    {
        var problem = CreateSixSessionProblem();
        var schedule = new Schedule();
        for (var sessionId = 1; sessionId <= 6; sessionId++)
        {
            schedule.Place(sessionId, (sessionId - 1) / 3);
        }

        var result = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedule);

        Assert.Equal(0.0, result.LoadStdDev, Precision);
    }

    [Fact]
    public void UnevenLoadHasPositiveStdDev()
    {
        var problem = CreateSixSessionProblem();
        var schedule = new Schedule();
        for (var sessionId = 1; sessionId <= 6; sessionId++)
        {
            // 4 phiên ở mốc đầu, 2 phiên ở mốc sau: tải 4 và 2, trung bình 3, độ lệch chuẩn 1.
            schedule.Place(sessionId, sessionId <= 4 ? 0 : 1);
        }

        var result = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedule);

        Assert.Equal(1.0, result.LoadStdDev, Precision);
        Assert.Equal(0.5, result.NormalizedLoad, Precision);
    }

    // ---------- LỊCH TRỐNG, PHIÊN CHƯA ĐẶT, PHIÊN CHƯA CÓ PHÒNG ----------
    [Fact]
    public void EmptyScheduleHasAllZeroValues()
    {
        var calculator = new ObjectiveCalculator(ExampleProblem.Create(), ObjectiveWeights.Default);

        var result = calculator.Evaluate(new Schedule());

        Assert.Equal(0, result.ConflictCount);
        Assert.Equal(0.0, result.LoadStdDev, Precision);
        Assert.Equal(0, result.WastedSeats);
        Assert.Equal(0.0, result.Value, Precision);
    }

    [Fact]
    public void UnplacedSessionsDoNotCount()
    {
        var calculator = new ObjectiveCalculator(ExampleProblem.Create(), ObjectiveWeights.Default);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);

        var result = calculator.Evaluate(schedule);

        // Chỉ có A được đặt: không có cặp nào để xung đột, S chỉ tính A (100 - 42).
        Assert.Equal(0, result.ConflictCount);
        Assert.Equal(58, result.WastedSeats);
    }

    [Fact]
    public void PlacedSessionWithoutRoomHasNoWastedSeats()
    {
        var calculator = new ObjectiveCalculator(ExampleProblem.Create(), ObjectiveWeights.Default);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock);
        schedule.Place(ExampleProblem.SessionC, ExampleProblem.NineOClock);

        var result = calculator.Evaluate(schedule);

        Assert.Equal(35, result.ConflictCount);
        Assert.Equal(0, result.WastedSeats);
    }

    // ---------- TRỌNG SỐ ----------
    [Fact]
    public void DefaultWeightsAre7020And10Percent()
    {
        Assert.Equal(0.7, ObjectiveWeights.Default.Conflict, Precision);
        Assert.Equal(0.2, ObjectiveWeights.Default.Load, Precision);
        Assert.Equal(0.1, ObjectiveWeights.Default.Waste, Precision);
    }

    [Fact]
    public void NegativeWeightIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ObjectiveWeights(1.2, -0.2, 0.0);
        });
    }

    [Fact]
    public void WeightsNotSummingToOneAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new ObjectiveWeights(0.5, 0.2, 0.1);
        });
    }

    [Fact]
    public void ChangingWeightsChangesValueByTheFormula()
    {
        var problem = ExampleProblem.Create();
        var schedule = ExampleProblem.CreateGreedySchedule();

        var onlyConflict = new ObjectiveCalculator(problem, new ObjectiveWeights(1.0, 0.0, 0.0)).Evaluate(schedule);
        var onlyWaste = new ObjectiveCalculator(problem, new ObjectiveWeights(0.0, 0.0, 1.0)).Evaluate(schedule);

        Assert.Equal(0.875, onlyConflict.Value, Precision);
        Assert.Equal(164.0 / 284.0, onlyWaste.Value, Precision);
    }

    // ---------- CHUẨN HÓA TRÊN LỊCH NGẪU NHIÊN HỢP LỆ ----------
    [Fact]
    public void NormalizedValuesStayBetweenZeroAndOne()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = seed }).Problem;
            var schedule = RandomScheduleBuilder.Build(problem, seed);

            var result = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedule);

            Assert.InRange(result.NormalizedConflict, 0.0, 1.0);
            Assert.InRange(result.NormalizedLoad, 0.0, 1.0);
            Assert.InRange(result.NormalizedWaste, 0.0, 1.0);
            Assert.InRange(result.Value, 0.0, 1.0);
        }
    }

    // 6 phiên 60 phút không có diễn giả, 4 phòng, 2 mốc (9h và 10h).
    private static SchedulingProblem CreateSixSessionProblem()
    {
        var sessions = new List<SchedulingSession>();
        for (var sessionId = 1; sessionId <= 6; sessionId++)
        {
            sessions.Add(TestData.Session(sessionId, 60));
        }

        var rooms = new List<SchedulingRoom>();
        for (var roomId = 1; roomId <= 4; roomId++)
        {
            rooms.Add(TestData.Room(roomId, 50));
        }

        return TestData.CreateProblem(sessions, rooms, [TestData.Day(540, 660)], null, null, 60);
    }
}
