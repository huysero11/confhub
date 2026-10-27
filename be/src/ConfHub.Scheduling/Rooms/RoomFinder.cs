using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rooms;

// Tìm phòng trống suốt một khoảng, giữ thứ tự phòng của bài toán.
// Hungarian bỏ qua nhóm đang gán lại; M1 chỉ tìm phòng cho phiên mới.
public static class RoomFinder
{
    // Dùng ở: GroupRoomAssigner.TryAssign, FirstFitScheduler.TryPlaceSession (lọc phòng đang bị chiếm).
    // Tìm những phiên đã được đặt trong lịch xem phiên nào có interval overlaps với session đang xét, đóng còn lại sẽ thỏa mãn
    public static IReadOnlyList<int> FindFreeRoomIds(
        SchedulingProblem problem,
        Schedule schedule,
        int dayIndex,
        TimeInterval interval,
        IReadOnlyCollection<int> excludedSessionIds)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(excludedSessionIds);

        var occupiedRoomIds = new HashSet<int>();
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            if (excludedSessionIds.Contains(sessionId)
                || !schedule.TryGetPlacement(sessionId, out var placement)
                || !placement.RoomId.HasValue)
            {
                continue;
            }

            var (placedDayIndex, placedInterval) = problem.GetSessionInterval(sessionId, placement.SlotIndex);
            if (placedDayIndex == dayIndex && placedInterval.Overlaps(interval))
            {
                occupiedRoomIds.Add(placement.RoomId.Value);
            }
        }

        var freeRoomIds = new List<int>();
        foreach (var room in problem.Rooms)
        {
            if (!occupiedRoomIds.Contains(room.Id))
            {
                freeRoomIds.Add(room.Id);
            }
        }

        return freeRoomIds;
    }
}
