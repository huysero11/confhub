// Kiểm bước gán lại phòng toàn lịch: chỉ nhận khi f (hoặc f') giảm, giữ mốc, giữ phiên ghim.
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rescheduling;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class RoomReassignerTests
{
    private const int SessionS1 = 1;
    private const int Room40 = 40;
    private const int Room50 = 50;
    private const int Room100 = 100;

    // ---------- VÍ DỤ TÍNH TAY ----------
    // Ví dụ 6.5: mỗi khung giờ dùng đủ hai phòng nên tổng sức chứa không đổi, không có gì để gán lại.
    [Fact]
    public void ExampleSchedulesHaveNothingToImprove()
    {
        var problem = ExampleProblem.Create();
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);

        Assert.Null(RoomReassigner.TryImprove(problem, ExampleProblem.CreateGreedySchedule(), calculator));
        Assert.Null(RoomReassigner.TryImprove(problem, ExampleProblem.CreateSwappedSchedule(), calculator));
    }

    // Một phiên 30 người ngồi P100 trong khi P40 trống: gán lại vào P40, S 70 → 10; ghim thì giữ nguyên.
    [Fact]
    public void OversizedRoomIsReassignedUnlessLocked()
    {
        var problem = CreateSingleSessionProblem();
        var schedule = CreateSingleSessionSchedule();
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
        var before = calculator.Evaluate(schedule);

        var reassignedSchedule = RoomReassigner.TryImprove(problem, schedule, calculator);

        Assert.NotNull(reassignedSchedule);
        Assert.True(reassignedSchedule.TryGetPlacement(SessionS1, out var placement));
        Assert.Equal(new Placement(0, Room40), placement);
        var after = calculator.Evaluate(reassignedSchedule);
        Assert.Equal(70, before.WastedSeats);
        Assert.Equal(10, after.WastedSeats);
        Assert.Equal(before.ConflictCount, after.ConflictCount);
        Assert.Equal(before.LoadStdDev, after.LoadStdDev, 12);
        LocalSearchTests.AssertSameSchedule(CreateSingleSessionSchedule(), schedule);

        Assert.Null(RoomReassigner.TryImprove(problem, schedule, calculator, new HashSet<int> { SessionS1 }));
    }

    // Ca X / Y / Z: thứ tự theo giờ thất bại, thứ tự theo quy mô cho đúng S = 82 → không tốt hơn.
    [Fact]
    public void NoOrderBeatsTheXyzSchedule()
    {
        var problem = XyzCase.CreateProblem();
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);

        Assert.Null(RoomReassigner.TryImprove(problem, XyzCase.CreateSchedule(), calculator));
    }

    // ---------- PHẠT XÁO TRỘN KHI XẾP LẠI ----------
    // Xếp lại không biến động: w₄ lớn thì đổi phòng (D = 1) không bù được phần giảm S → giữ P100;
    // w₄ rất nhỏ thì phần giảm S thắng → chuyển sang P40.
    [Fact]
    public void DisruptionWeightDecidesWhetherRescheduleChangesRoom()
    {
        var problem = CreateSingleSessionProblem();
        var baseSchedule = CreateSingleSessionSchedule();

        var strict = new Rescheduler(problem, new RescheduleOptions { DisruptionWeight = 0.5 }).Run(baseSchedule);
        Assert.True(strict.Schedule.TryGetPlacement(SessionS1, out var strictPlacement));
        Assert.Equal(new Placement(0, Room100), strictPlacement);
        Assert.Empty(strict.ChangedSessionIds);

        var loose = new Rescheduler(problem, new RescheduleOptions { DisruptionWeight = 0.01 }).Run(baseSchedule);
        Assert.True(loose.Schedule.TryGetPlacement(SessionS1, out var loosePlacement));
        Assert.Equal(new Placement(0, Room40), loosePlacement);
        Assert.Equal([SessionS1], loose.ChangedSessionIds);
    }

    // ---------- DỮ LIỆU SINH ----------
    // Gán lại trên lịch tham lam (nếu có) chỉ giảm S, giữ C, B và hợp lệ; lịch M3 không còn gì để gán lại.
    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(100)]
    public void GeneratedSchedulesKeepTimesAndM3IsAFixedPoint(int sessionCount)
    {
        var generatorOptions = sessionCount switch
        {
            30 => GeneratorOptions.Small,
            60 => GeneratorOptions.Medium,
            _ => GeneratorOptions.Large,
        };
        var problem = ProblemGenerator.Generate(generatorOptions).Problem;
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
        var checker = new ConstraintChecker(problem);

        var greedySchedule = new GreedyScheduler(problem, ObjectiveWeights.Default).Run().Schedule;
        var reassignedSchedule = RoomReassigner.TryImprove(problem, greedySchedule, calculator);
        if (reassignedSchedule is not null)
        {
            var before = calculator.Evaluate(greedySchedule);
            var after = calculator.Evaluate(reassignedSchedule);
            Assert.Empty(checker.Validate(reassignedSchedule));
            Assert.Equal(before.ConflictCount, after.ConflictCount);
            Assert.Equal(before.LoadStdDev, after.LoadStdDev, 12);
            Assert.True(after.WastedSeats < before.WastedSeats);
            foreach (var sessionId in greedySchedule.PlacedSessionIds)
            {
                greedySchedule.TryGetPlacement(sessionId, out var oldPlacement);
                Assert.True(reassignedSchedule.TryGetPlacement(sessionId, out var newPlacement));
                Assert.Equal(oldPlacement.SlotIndex, newPlacement.SlotIndex);
            }
        }

        var localSchedule = new LocalSearchScheduler(problem).Run().Schedule;
        Assert.Empty(checker.Validate(localSchedule));
        Assert.Null(RoomReassigner.TryImprove(problem, localSchedule, calculator));
    }

    // ---------- HỖ TRỢ DỮ LIỆU ----------
    // Helper của các test (cùng lớp): một phiên 30 người, một khung 9h–10h, ba phòng 40 / 50 / 100.
    private static SchedulingProblem CreateSingleSessionProblem()
    {
        return TestData.CreateProblem(
            [TestData.SessionWithSize(SessionS1, 60, 30)],
            [TestData.Room(Room40, 40), TestData.Room(Room50, 50), TestData.Room(Room100, 100)],
            [TestData.Day(540, 600)],
            slotMinutes: 60);
    }

    // Helper của các test (cùng lớp): phiên S1 ngồi phòng 100 chỗ.
    private static Schedule CreateSingleSessionSchedule()
    {
        var schedule = new Schedule();
        schedule.Place(SessionS1, 0, Room100);
        return schedule;
    }
}
