// Gán phòng "nhỏ nhất đủ chỗ" cho các phiên đã có mốc, lần lượt theo một thứ tự cho trước.
// Giữ nguyên mốc; phiên ghim giữ phòng cũ và được đặt trước mọi phiên khác.
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rooms;

public static class BestFitRoomAssigner
{
    // Lấy lần lượt từng phiên trong sessionOrder, chọn phòng nhỏ nhất đủ chỗ còn trống suốt khoảng của nó;
    // phiên không còn phòng nào thì ghi vào FailedSessionIds và không có trong lịch kết quả.
    public static RoomBaselineResult Assign(
        SchedulingProblem problem,
        Schedule schedule,
        IReadOnlyList<int> sessionOrder,
        IReadOnlySet<int>? lockedSessionIds = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(sessionOrder);

        var assignedSchedule = new Schedule();
        var failedSessionIds = new List<int>();
        var wastedSeats = 0;

        // Phiên ghim đặt trước với đúng phòng cũ để phiên khác không lấy mất phòng đó.
        if (lockedSessionIds is not null)
        {
            foreach (var lockedSessionId in lockedSessionIds)
            {
                if (schedule.TryGetPlacement(lockedSessionId, out var placement) && placement.RoomId.HasValue)
                {
                    assignedSchedule.Place(lockedSessionId, placement.SlotIndex, placement.RoomId.Value);
                    wastedSeats += problem.GetRoom(placement.RoomId.Value).Capacity - problem.GetSessionSize(lockedSessionId);
                }
            }
        }

        foreach (var sessionId in sessionOrder)
        {
            // Đã đặt rồi (phiên ghim)
            if (assignedSchedule.IsPlaced(sessionId))
            {
                continue;
            }

            if (!schedule.TryGetPlacement(sessionId, out var placement))
            {
                throw new ArgumentException($"Phiên {sessionId} chưa có mốc trong lịch.", nameof(sessionOrder));
            }

            var selectedRoom = FindSmallestFittingRoom(problem, assignedSchedule, sessionId, placement.SlotIndex);
            if (selectedRoom is null)
            {
                failedSessionIds.Add(sessionId);
                continue;
            }

            assignedSchedule.Place(sessionId, placement.SlotIndex, selectedRoom.Id);
            wastedSeats += selectedRoom.Capacity - problem.GetSessionSize(sessionId);
        }

        return new RoomBaselineResult(assignedSchedule, wastedSeats, failedSessionIds);
    }

    // Helper của Assign (cùng lớp): tìm phòng trống nhỏ nhất đủ chỗ cho phiên tại mốc đã cho; trả null nếu không có phòng nào.
    private static SchedulingRoom? FindSmallestFittingRoom(
        SchedulingProblem problem,
        Schedule assignedSchedule,
        int sessionId,
        int slotIndex)
    {
        var (dayIndex, interval) = problem.GetSessionInterval(sessionId, slotIndex);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(problem, assignedSchedule, dayIndex, interval, []);
        var sessionSize = problem.GetSessionSize(sessionId);

        SchedulingRoom? bestRoom = null;
        foreach (var roomId in freeRoomIds)
        {
            var room = problem.GetRoom(roomId);
            if (room.Capacity >= sessionSize && (bestRoom is null || room.Capacity < bestRoom.Capacity))
            {
                bestRoom = room;
            }
        }

        return bestRoom;
    }
}
