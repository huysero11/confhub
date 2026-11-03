// Kiểm chứng gán phòng tham lam, ca thất bại do chồng khoảng và tính bất biến của lịch vào.
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class GreedyRoomBaselineTests
{
    // Ví dụ 6.5 có cùng phòng với Hungarian và S = 164.
    [Fact]
    public void ExampleMatchesHungarianRooms()
    {
        var problem = ExampleProblem.Create();
        var schedule = ExampleProblem.CreateGreedySchedule();
        var assignment = GreedyRoomBaseline.Assign(problem, schedule);
        Assert.Empty(assignment.FailedSessionIds);
        Assert.Equal(164, assignment.WastedSeats);
        Assert.Empty(new ConstraintChecker(problem).Validate(assignment.Schedule));
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            schedule.TryGetPlacement(sessionId, out var originalPlacement);
            assignment.Schedule.TryGetPlacement(sessionId, out var assignedPlacement);
            Assert.Equal(originalPlacement, assignedPlacement);
        }
    }

    // Chọn phòng nhỏ cho X khiến Y chiếm P100 và Z thất bại, đúng S = 72 / 82.
    [Fact]
    public void OverlappingIntervalsCanDefeatGreedyWithoutMutatingInput()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 30), TestData.SessionWithSize(2, 60, 33), TestData.SessionWithSize(3, 60, 90)],
            [TestData.Room(35, 35), TestData.Room(100, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 30);
        var schedule = new Schedule();
        schedule.Place(3, 2, 100);
        schedule.Place(2, 1, 35);
        schedule.Place(1, 0, 100);
        var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
        Assert.Empty(new ConstraintChecker(problem).Validate(schedule));
        Assert.Equal(82, calculator.Evaluate(schedule).WastedSeats);

        var assignment = GreedyRoomBaseline.Assign(problem, schedule);

        Assert.Equal(3, Assert.Single(assignment.FailedSessionIds));
        Assert.Equal(72, assignment.WastedSeats);
        Assert.Equal(72, calculator.Evaluate(assignment.Schedule).WastedSeats);
        Assert.True(assignment.Schedule.TryGetPlacement(1, out var firstPlacement));
        Assert.True(assignment.Schedule.TryGetPlacement(2, out var secondPlacement));
        Assert.Equal(new Placement(0, 35), firstPlacement);
        Assert.Equal(new Placement(1, 100), secondPlacement);
        Assert.False(assignment.Schedule.IsPlaced(3));
        Assert.Empty(new ConstraintChecker(problem).Validate(assignment.Schedule));
        Assert.Equal(3, schedule.PlacedSessionIds.Count);
        Assert.Equal(3, schedule.PlacedSessionIds[0]);
        Assert.Equal(2, schedule.PlacedSessionIds[1]);
        Assert.Equal(1, schedule.PlacedSessionIds[2]);
        schedule.TryGetPlacement(1, out var originalFirst);
        schedule.TryGetPlacement(2, out var originalSecond);
        schedule.TryGetPlacement(3, out var originalThird);
        Assert.Equal(new Placement(0, 100), originalFirst);
        Assert.Equal(new Placement(1, 35), originalSecond);
        Assert.Equal(new Placement(2, 100), originalThird);
        Assert.Equal(82, calculator.Evaluate(schedule).WastedSeats);
    }

    // Phiên chưa có mốc không tự được gán, lịch rỗng vẫn rỗng.
    [Fact]
    public void UnplacedSessionsAreNotAssigned()
    {
        var problem = ExampleProblem.Create();
        var schedule = new Schedule();
        var emptyAssignment = GreedyRoomBaseline.Assign(problem, schedule);
        Assert.Empty(emptyAssignment.Schedule.PlacedSessionIds);
        Assert.Empty(emptyAssignment.FailedSessionIds);
        Assert.Equal(0, emptyAssignment.WastedSeats);

        schedule.Place(ExampleProblem.SessionA, 0);
        var assignment = GreedyRoomBaseline.Assign(problem, schedule);
        Assert.Equal(ExampleProblem.SessionA, Assert.Single(assignment.Schedule.PlacedSessionIds));
        Assert.Empty(assignment.FailedSessionIds);
        schedule.TryGetPlacement(ExampleProblem.SessionA, out var originalPlacement);
        Assert.Null(originalPlacement.RoomId);
    }

    // Cùng mốc lấy thứ tự nhập, không lấy thứ tự đặt lịch; phòng bằng sức chứa cũng giữ thứ tự nhập.
    [Fact]
    public void TiesUseInputOrderForSessionsAndRooms()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(2, 60, 10), TestData.SessionWithSize(1, 60, 10)],
            [TestData.Room(20, 20), TestData.Room(10, 20)],
            [TestData.Day(540, 600)]);
        var schedule = new Schedule();
        schedule.Place(1, 0, 20);
        schedule.Place(2, 0, 10);
        var assignment = GreedyRoomBaseline.Assign(problem, schedule);
        assignment.Schedule.TryGetPlacement(2, out var firstPlacement);
        assignment.Schedule.TryGetPlacement(1, out var secondPlacement);
        Assert.Equal(20, firstPlacement.RoomId);
        Assert.Equal(10, secondPlacement.RoomId);
    }
}
