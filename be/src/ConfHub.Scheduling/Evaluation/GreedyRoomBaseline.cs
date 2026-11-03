// Mốc so sánh gán phòng: đi theo thời gian, chọn phòng nhỏ nhất đủ chỗ và không xét lại.
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Evaluation;

public static class GreedyRoomBaseline
{
    // Gán lại các phiên đã có mốc; lịch đầu vào giữ nguyên kể cả khi có phiên gán thất bại.
    public static RoomBaselineResult Assign(SchedulingProblem problem, Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);
        var assignedSchedule = new Schedule();
        var failedSessionIds = new List<int>();
        var wastedSeats = 0;

        // Timeline theo ngày / giờ; vòng trong giữ thứ tự nhập khi nhiều phiên cùng mốc.
        foreach (var slot in problem.Timeline.Slots)
        {
            foreach (var session in problem.Sessions)
            {
                if (!schedule.TryGetPlacement(session.Id, out var placement) || placement.SlotIndex != slot.Index)
                {
                    continue;
                }

                var (dayIndex, interval) = problem.GetSessionInterval(session.Id, placement.SlotIndex);
                var freeRoomIds = RoomFinder.FindFreeRoomIds(problem, assignedSchedule, dayIndex, interval, []);
                var sessionSize = problem.GetSessionSize(session.Id);
                SchedulingRoom? selectedRoom = null;
                foreach (var roomId in freeRoomIds)
                {
                    var room = problem.GetRoom(roomId);
                    if (room.Capacity >= sessionSize && (selectedRoom is null || room.Capacity < selectedRoom.Capacity))
                    {
                        selectedRoom = room;
                    }
                }

                if (selectedRoom is null)
                {
                    failedSessionIds.Add(session.Id);
                    continue;
                }

                assignedSchedule.Place(session.Id, placement.SlotIndex, selectedRoom.Id);
                wastedSeats += selectedRoom.Capacity - sessionSize;
            }
        }

        return new RoomBaselineResult(assignedSchedule, wastedSeats, failedSessionIds);
    }
}
