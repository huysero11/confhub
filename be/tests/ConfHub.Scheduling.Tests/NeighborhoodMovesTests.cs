using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm từng phép biến đổi độc lập, tính hợp lệ H1–H5 và việc giữ nguyên lịch truyền vào.
public class NeighborhoodMovesTests
{
    // ---------- VÍ DỤ 6.5 ----------
    // Dùng ở: dotnet test / Debug Test (chỉ C ↔ B hợp lệ trong các phép cải thiện của vòng đầu).
    [Fact]
    public void ExampleMovesMatchManualConstraintsAndNeverMutateInput()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = ExampleProblem.CreateGreedySchedule();
        var originalSchedule = schedule.Clone();

        Assert.Null(NeighborhoodMoves.TryMove(problem, checker, schedule, ExampleProblem.SessionC, ExampleProblem.TenOClock));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
        var swappedSchedule = NeighborhoodMoves.TrySwap(problem, checker, schedule, ExampleProblem.SessionC, ExampleProblem.SessionB);
        Assert.NotNull(swappedSchedule);
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
        LocalSearchTests.AssertSameSchedule(ExampleProblem.CreateSwappedSchedule(), swappedSchedule);
        Assert.Empty(checker.Validate(swappedSchedule));
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(swappedSchedule);
        Assert.Equal(5, objective.ConflictCount);
        Assert.Equal((0.7 * 5.0 / 40.0) + (0.1 * 164.0 / 284.0), objective.Value, 12);

        Assert.Null(NeighborhoodMoves.TrySwap(problem, checker, schedule, ExampleProblem.SessionA, ExampleProblem.SessionD));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
        Assert.Null(NeighborhoodMoves.TrySwap(problem, checker, schedule, ExampleProblem.SessionA, ExampleProblem.SessionB));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
        Assert.Null(NeighborhoodMoves.TrySwap(problem, checker, schedule, ExampleProblem.SessionC, ExampleProblem.SessionD));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
    }

    // ---------- DỜI VÀ GÁN PHÒNG ----------
    // Dùng ở: dotnet test (Hungarian có thể đổi phòng phiên cũ trong nhóm đích để nhận phiên lớn).
    [Fact]
    public void MoveReassignsTargetGroupAndLeavesSourceGroupAlone()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 80), TestData.SessionWithSize(2, 60, 20), TestData.SessionWithSize(3, 60, 20)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 1, 1);
        schedule.Place(3, 0, 2);
        var originalSchedule = schedule.Clone();
        var movedSchedule = NeighborhoodMoves.TryMove(problem, new ConstraintChecker(problem), schedule, 1, 1);
        Assert.NotNull(movedSchedule);
        Assert.True(movedSchedule.TryGetPlacement(1, out var movedPlacement));
        Assert.Equal(new Placement(1, 1), movedPlacement);
        Assert.True(movedSchedule.TryGetPlacement(2, out var targetPlacement));
        Assert.Equal(new Placement(1, 2), targetPlacement);
        Assert.True(movedSchedule.TryGetPlacement(3, out var sourcePlacement));
        Assert.Equal(new Placement(0, 2), sourcePlacement);
        Assert.Empty(new ConstraintChecker(problem).Validate(movedSchedule));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
    }

    // Dùng ở: dotnet test (H2 qua nhưng không còn phòng đủ sức chứa thì Hungarian từ chối).
    [Fact]
    public void MoveRejectsCapacityFailureAfterConcurrencyPasses()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 80), TestData.SessionWithSize(2, 60, 80)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 1, 1);
        var originalSchedule = schedule.Clone();
        Assert.Null(NeighborhoodMoves.TryMove(problem, new ConstraintChecker(problem), schedule, 1, 1));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
    }

    // ---------- RÀNG BUỘC THEO KHOẢNG ----------
    // Dùng ở: dotnet test (đổi hai phiên dài khác nhau phải kiểm cả phần thời gian mới giao nhau).
    [Fact]
    public void SwapRejectsNewRoomOverlapFromDifferentDurations()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 120), TestData.Session(2, 60), TestData.Session(3, 60)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 840)],
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 2, 2);
        schedule.Place(3, 3, 2);
        Assert.Empty(new ConstraintChecker(problem).Validate(schedule));
        var originalSchedule = schedule.Clone();
        Assert.Null(NeighborhoodMoves.TrySwap(problem, new ConstraintChecker(problem), schedule, 1, 2));
        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
    }

    // Dùng ở: dotnet test (mốc khác nhưng khoảng giao nhau vẫn bị Q3 loại).
    [Fact]
    public void SwapRejectsOverlappingIntervalsEvenAtDifferentStartSlots()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 120), TestData.Session(2, 60)],
            [TestData.Room(1, 100), TestData.Room(2, 100)],
            [TestData.Day(540, 780)],
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 1, 2);
        Assert.Null(NeighborhoodMoves.TrySwap(problem, new ConstraintChecker(problem), schedule, 1, 2));
    }

    // Dùng ở: dotnet test (cùng giờ ở hai ngày vẫn đổi được; giờ rảnh chỉ ngày cũ phải bị H5 loại).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossDaySwapRespectsSpeakerAvailability(bool isConstrained)
    {
        List<SpeakerAvailability> availabilities = [];
        if (isConstrained)
        {
            availabilities.Add(new SpeakerAvailability(10, 0, 540, 600));
        }

        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10), TestData.Session(2, 60, 20)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 600), TestData.Day(540, 600)],
            availabilities,
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 1, 1);
        var originalSchedule = schedule.Clone();
        var swappedSchedule = NeighborhoodMoves.TrySwap(problem, new ConstraintChecker(problem), schedule, 1, 2);
        if (isConstrained)
        {
            Assert.Null(swappedSchedule);
        }
        else
        {
            Assert.NotNull(swappedSchedule);
            Assert.Empty(new ConstraintChecker(problem).Validate(swappedSchedule));
        }

        LocalSearchTests.AssertSameSchedule(originalSchedule, schedule);
    }

    // Dùng ở: dotnet test (mốc hiện tại, mốc ngoài lịch, phiên chưa đặt và tự đổi đều không sinh bước).
    [Fact]
    public void MissingSessionsAndNoOpMovesReturnNull()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = ExampleProblem.CreateGreedySchedule();
        Assert.Null(NeighborhoodMoves.TryMove(problem, checker, schedule, 1, 0));
        Assert.Null(NeighborhoodMoves.TryMove(problem, checker, schedule, 1, -1));
        Assert.Null(NeighborhoodMoves.TrySwap(problem, checker, schedule, 1, 1));
        schedule.Remove(4);
        Assert.Null(NeighborhoodMoves.TryMove(problem, checker, schedule, 4, 1));
        Assert.Null(NeighborhoodMoves.TrySwap(problem, checker, schedule, 4, 3));
    }
}
