using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm Hungarian theo nhóm, giữ phòng khi hòa và không đụng phiên ngoài nhóm.
public class GroupRoomAssignerTests
{
    // Dùng ở: dotnet test (một phiên chọn phòng vừa nhất).
    [Fact]
    public void SingleSessionChoosesLeastWaste()
    {
        var assignment = GroupRoomAssigner.TryAssign(ExampleProblem.Create(), new Schedule(), ExampleProblem.SessionD, 1);
        Assert.True(assignment.IsFeasible);
        Assert.Null(assignment.FailureCode);
        Assert.Equal(ExampleProblem.RoomP102, assignment.RoomBySession[ExampleProblem.SessionD]);
    }

    // Dùng ở: dotnet test (bước xếp B lúc 10h của ví dụ 6.5).
    [Fact]
    public void ExampleKeepsDInP102AndAssignsBToP101()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionD, ExampleProblem.TenOClock, ExampleProblem.RoomP102);
        var assignment = GroupRoomAssigner.TryAssign(ExampleProblem.Create(), schedule, ExampleProblem.SessionB, 1);
        Assert.True(assignment.IsFeasible);
        Assert.Equal(ExampleProblem.RoomP102, assignment.RoomBySession[ExampleProblem.SessionD]);
        Assert.Equal(ExampleProblem.RoomP101, assignment.RoomBySession[ExampleProblem.SessionB]);
        Assert.False(schedule.IsPlaced(ExampleProblem.SessionB));
    }

    // Dùng ở: dotnet test (nhóm ba phiên chỉ có hai phòng).
    [Fact]
    public void ExampleCannotAddCAtTenBecauseOfH2()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionD, 1, ExampleProblem.RoomP102);
        schedule.Place(ExampleProblem.SessionB, 1, ExampleProblem.RoomP101);
        var assignment = GroupRoomAssigner.TryAssign(ExampleProblem.Create(), schedule, ExampleProblem.SessionC, 1);
        Assert.False(assignment.IsFeasible);
        Assert.Equal(ConstraintCode.H2, assignment.FailureCode);
        Assert.Empty(assignment.RoomBySession);
    }

    // Dùng ở: dotnet test (đủ số phòng nhưng không ghép được vì sức chứa).
    [Fact]
    public void OversizedSessionFailsWithH4()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 101)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)]);
        var assignment = GroupRoomAssigner.TryAssign(problem, new Schedule(), 1, 0);
        Assert.False(assignment.IsFeasible);
        Assert.Equal(ConstraintCode.H4, assignment.FailureCode);
        Assert.Empty(assignment.RoomBySession);
    }

    // Dùng ở: dotnet test (phiên nhỏ nhường phòng lớn cho phiên mới cùng khoảng).
    [Fact]
    public void SmallSessionMovesToMakeRoomForLargeSession()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionC, 0, ExampleProblem.RoomP101);
        var assignment = GroupRoomAssigner.TryAssign(ExampleProblem.Create(), schedule, ExampleProblem.SessionA, 0);
        Assert.True(assignment.IsFeasible);
        Assert.Equal(ExampleProblem.RoomP102, assignment.RoomBySession[ExampleProblem.SessionC]);
        Assert.Equal(ExampleProblem.RoomP101, assignment.RoomBySession[ExampleProblem.SessionA]);
        Assert.True(schedule.TryGetPlacement(ExampleProblem.SessionC, out var originalPlacement));
        Assert.Equal(ExampleProblem.RoomP101, originalPlacement.RoomId);
    }

    // Dùng ở: dotnet test (chồng một phần không được lấy phòng của phiên ngoài nhóm).
    [Theory]
    [InlineData(90, 0)]
    [InlineData(60, 1)]
    public void PartiallyOverlappingSessionCannotJoinGroup(int existingDuration, int candidateSlot)
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, existingDuration, 20), TestData.SessionWithSize(2, 60, 80)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 720)]);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        var assignment = GroupRoomAssigner.TryAssign(problem, schedule, 2, candidateSlot);
        Assert.False(assignment.IsFeasible);
        Assert.Equal(ConstraintCode.H4, assignment.FailureCode);
        Assert.True(schedule.TryGetPlacement(1, out var originalPlacement));
        Assert.Equal(1, originalPlacement.RoomId);
    }

    // Dùng ở: dotnet test (hai phiên lớn không thể dùng chung phòng lớn dù mỗi hàng đều có ô khả thi).
    [Fact]
    public void HallConflictFailsWithH4()
    {
        var problem = TestData.CreateProblem(
            [TestData.SessionWithSize(1, 60, 80), TestData.SessionWithSize(2, 60, 80)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 660)]);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        var assignment = GroupRoomAssigner.TryAssign(problem, schedule, 2, 0);
        Assert.False(assignment.IsFeasible);
        Assert.Equal(ConstraintCode.H4, assignment.FailureCode);
    }
}
