using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm phòng trống theo khoảng nửa mở và theo ngày, không chỉ cùng mốc bắt đầu.
public class RoomFinderTests
{
    // Dùng ở: dotnet test (phiên bắt đầu trước khoảng đang hỏi vẫn chiếm phòng).
    [Fact]
    public void OverlappingSessionOccupiesItsRoom()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(ExampleProblem.Create(), schedule, 0, new TimeInterval(570, 630), []);
        Assert.Equal([ExampleProblem.RoomP102], freeRoomIds);
    }

    // Dùng ở: dotnet test (chạm đầu mút không chồng; khác ngày không chiếm).
    [Theory]
    [InlineData(0, 600, 660)]
    [InlineData(0, 480, 540)]
    [InlineData(1, 540, 600)]
    public void TouchingEndpointsOrAnotherDayLeavesRoomsFree(int dayIndex, int startMinute, int endMinute)
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(
            ExampleProblem.Create(), schedule, dayIndex, new TimeInterval(startMinute, endMinute), []);
        Assert.Equal([ExampleProblem.RoomP101, ExampleProblem.RoomP102], freeRoomIds);
    }

    // Dùng ở: dotnet test (phiên chưa gán phòng và phiên loại trừ không giữ phòng).
    [Fact]
    public void UnassignedAndExcludedSessionsDoNotOccupyRooms()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);
        schedule.Place(ExampleProblem.SessionC, ExampleProblem.NineOClock);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(
            ExampleProblem.Create(), schedule, 0, new TimeInterval(540, 600), [ExampleProblem.SessionA]);
        Assert.Equal([ExampleProblem.RoomP101, ExampleProblem.RoomP102], freeRoomIds);
    }

    // Dùng ở: dotnet test (M1 cần thứ tự nhập, không sắp phòng theo mã hay sức chứa).
    [Fact]
    public void RoomInputOrderIsPreserved()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60)],
            [TestData.Room(9, 70), TestData.Room(2, 100), TestData.Room(7, 30)],
            [TestData.Day(540, 660)]);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(problem, new Schedule(), 0, new TimeInterval(540, 600), []);
        Assert.Equal([9, 2, 7], freeRoomIds);
    }
}
