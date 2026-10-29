using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rooms;

// Gán phòng tối ưu cho nhóm có đúng cùng ngày, giờ bắt đầu và giờ kết thúc.
// Chỉ trả đề xuất, không sửa lịch; các phiên chồng giờ ngoài nhóm giữ nguyên phòng.
public static class GroupRoomAssigner
{
    // Dùng ở: GreedyScheduler.TryPlaceSession (thử chia phòng trước khi chấm f); T2.3 sau khi dời / hoán đổi.
    // Dùng ở: NeighborhoodMoves.TryMove (gán phòng cho nhóm đích sau khi gỡ phiên khỏi vị trí cũ).
    public static RoomAssignment TryAssign(SchedulingProblem problem, Schedule schedule, int sessionId, int slotIndex)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);

        var (dayIndex, interval) = problem.GetSessionInterval(sessionId, slotIndex);
        var groupSessionIds = BuildGroupSessionIds(problem, schedule, sessionId, dayIndex, interval);
        var freeRoomIds = RoomFinder.FindFreeRoomIds(problem, schedule, dayIndex, interval, groupSessionIds);

        // Nhóm đông hơn số phòng ứng viên thì không chia được: H2 (hết phòng).
        if (groupSessionIds.Count > freeRoomIds.Count)
        {
            return new RoomAssignment(false, ConstraintCode.H2, new Dictionary<int, int>());
        }

        var costs = BuildCosts(problem, schedule, groupSessionIds, freeRoomIds);
        var assignedColumns = HungarianAlgorithm.Solve(costs); // assignedColumns[i] = j, thì j là index của freeRoomIds

        var roomBySession = new Dictionary<int, int>(); // {sessionId, roomId}
        for (var i = 0; i < groupSessionIds.Count; i++)
        {
            var groupSessionId = groupSessionIds[i];
            var j = assignedColumns[i];

            if (costs[i][j] == HungarianAlgorithm.Infeasible)
            {
                return new RoomAssignment(false, ConstraintCode.H4, new Dictionary<int, int>());
            }

            roomBySession[groupSessionId] = freeRoomIds[j];
        }

        return new RoomAssignment(true, null, roomBySession);
    }

    // Helper của TryAssign (cùng lớp).
    // Lấy list các id của các phiên cùng ngày cùng giờ với sessionId
    private static List<int> BuildGroupSessionIds(SchedulingProblem problem, Schedule schedule, int sessionId, int dayIndex, TimeInterval interval)
    {
        var groupSessionIds = new List<int>();
        foreach (var placedSessionId in schedule.PlacedSessionIds)
        {
            if (placedSessionId == sessionId || !schedule.TryGetPlacement(placedSessionId, out var placement))
            {
                continue;
            }

            var (placedDayIndex, placedInterval) = problem.GetSessionInterval(placedSessionId, placement.SlotIndex);
            if (placedDayIndex == dayIndex && interval == placedInterval)
            {
                groupSessionIds.Add(placedSessionId);
            }
        }

        groupSessionIds.Add(sessionId);

        return groupSessionIds;
    }

    // Helper của TryAssign (cùng lớp): hàng là phiên trong nhóm, cột là phòng ứng viên.
    private static long[][] BuildCosts(SchedulingProblem problem, Schedule schedule, List<int> groupSessionIds, IReadOnlyList<int> freeRoomIds)
    {
        var costs = new long[groupSessionIds.Count][];
        for (var row = 0; row < groupSessionIds.Count; row++)
        {
            var sessionId = groupSessionIds[row];
            var sessionSize = problem.GetSessionSize(sessionId);
            schedule.TryGetPlacement(sessionId, out var placement);

            costs[row] = new long[freeRoomIds.Count];
            for (var col = 0; col < freeRoomIds.Count; col++)
            {
                var roomId = freeRoomIds[col];
                var capacity = problem.GetRoom(roomId).Capacity;

                // Phòng không đủ chỗ
                if (capacity < sessionSize)
                {
                    costs[row][col] = HungarianAlgorithm.Infeasible;
                    continue;
                }

                /*
                    Nhân 1000 để lãng phí ghế là tiêu chí chính, nếu không thì việc đổi phòng và thừa 1 ghế đều có giá trị ngang nhau
                    VD: 10 ghế và đổi phòng 2 lần => 10 000 + 2 = 10 002
                        11 ghế và đổi phòng 0 lần => 11 000 + 0 = 11 000
                        => ưu tiên chọn đổi phòng vì nó là cách sắp xếp chứ không mất chi phí
                */
                costs[row][col] = (capacity - (long)sessionSize) * 1000;

                // Nếu session này đã có phòng rồi, thì +1 để tránh đổi phòng mà không có ý nghĩa
                if (placement.RoomId.HasValue && placement.RoomId.Value != roomId)
                {
                    costs[row][col]++;
                }
            }
        }

        return costs;
    }
}
