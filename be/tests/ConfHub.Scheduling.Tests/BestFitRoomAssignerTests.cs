// Kiểm hai thứ tự gán phòng nhỏ nhất đủ chỗ, phiên ghim giữ phòng và lịch vào không bị sửa.
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class BestFitRoomAssignerTests
{
    // ---------- HAI THỨ TỰ ----------
    // Ca X / Y / Z (T2.4 mục 3.4): theo giờ thì Z thất bại; phiên đông trước thì gán đủ, S = 82.
    [Fact]
    public void StartOrderFailsButSizeOrderAssignsEverySession()
    {
        var problem = XyzCase.CreateProblem();
        var schedule = XyzCase.CreateSchedule();

        var startOrder = RoomReassigner.OrderByStart(problem, schedule);
        var sizeOrder = RoomReassigner.OrderBySizeDescending(problem, schedule);
        Assert.Equal([XyzCase.SessionX, XyzCase.SessionY, XyzCase.SessionZ], startOrder);
        Assert.Equal([XyzCase.SessionZ, XyzCase.SessionY, XyzCase.SessionX], sizeOrder);

        var byStart = BestFitRoomAssigner.Assign(problem, schedule, startOrder);
        Assert.Equal([XyzCase.SessionZ], byStart.FailedSessionIds);
        Assert.Equal(72, byStart.WastedSeats);

        var bySize = BestFitRoomAssigner.Assign(problem, schedule, sizeOrder);
        Assert.Empty(bySize.FailedSessionIds);
        Assert.Equal(82, bySize.WastedSeats);
        AssertRoom(bySize.Schedule, XyzCase.SessionZ, XyzCase.Room100);
        AssertRoom(bySize.Schedule, XyzCase.SessionY, XyzCase.Room35);
        AssertRoom(bySize.Schedule, XyzCase.SessionX, XyzCase.Room100);

        // Lịch vào giữ nguyên phòng cũ.
        LocalSearchTests.AssertSameSchedule(XyzCase.CreateSchedule(), schedule);
    }

    // ---------- PHIÊN GHIM VÀ ĐẦU VÀO SAI ----------
    // X ghim ở P100 nên được đặt trước; theo giờ Y vào P35, Z vào P100 (X đã xong lúc 10h).
    [Fact]
    public void LockedSessionKeepsItsRoom()
    {
        var problem = XyzCase.CreateProblem();
        var schedule = XyzCase.CreateSchedule();

        var assignment = BestFitRoomAssigner.Assign(
            problem,
            schedule,
            RoomReassigner.OrderByStart(problem, schedule),
            new HashSet<int> { XyzCase.SessionX });

        Assert.Empty(assignment.FailedSessionIds);
        Assert.Equal(82, assignment.WastedSeats);
        AssertRoom(assignment.Schedule, XyzCase.SessionX, XyzCase.Room100);
        AssertRoom(assignment.Schedule, XyzCase.SessionY, XyzCase.Room35);
        AssertRoom(assignment.Schedule, XyzCase.SessionZ, XyzCase.Room100);
    }

    // Thứ tự chứa phiên chưa có mốc là lỗi của tầng gọi.
    [Fact]
    public void UnplacedSessionInOrderIsRejected()
    {
        var problem = XyzCase.CreateProblem();
        var schedule = new Schedule();
        schedule.Place(XyzCase.SessionX, 0, XyzCase.Room100);

        Assert.Throws<ArgumentException>(() => BestFitRoomAssigner.Assign(problem, schedule, [XyzCase.SessionX, XyzCase.SessionY]));
    }

    // Helper của các test (cùng lớp): phiên phải có đúng phòng mong đợi.
    private static void AssertRoom(Schedule schedule, int sessionId, int roomId)
    {
        Assert.True(schedule.TryGetPlacement(sessionId, out var placement));
        Assert.Equal(roomId, placement.RoomId);
    }
}
