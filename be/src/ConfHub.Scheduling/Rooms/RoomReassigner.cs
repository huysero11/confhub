// Gán lại phòng cho toàn lịch: giữ nguyên mốc của mọi phiên, thử hai thứ tự "phòng nhỏ nhất đủ chỗ"
// và chỉ nhận khi f (hoặc f' khi xếp lại) giảm. Mốc không đổi nên C và B không đổi, chỉ S thay đổi.
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rooms;

public static class RoomReassigner
{
    private const double ScoreTolerance = 1e-12;

    // Trả lịch mới có f nhỏ hơn lịch vào, hoặc null nếu không thứ tự nào tốt hơn; lịch vào không bị sửa.
    public static Schedule? TryImprove(
        SchedulingProblem problem,
        Schedule schedule,
        ObjectiveCalculator calculator,
        IReadOnlySet<int>? lockedSessionIds = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(calculator);

        var bestScore = calculator.Evaluate(schedule).Value;
        Schedule? bestSchedule = null;
        IReadOnlyList<int>[] sessionOrders = [OrderByStart(problem, schedule), OrderBySizeDescending(problem, schedule)];
        foreach (var sessionOrder in sessionOrders)
        {
            var assignment = BestFitRoomAssigner.Assign(problem, schedule, sessionOrder, lockedSessionIds);

            // Thứ tự nào làm có phiên mất phòng thì bỏ: không đổi ít ghế thừa lấy một phiên chưa xếp.
            if (assignment.FailedSessionIds.Count > 0)
            {
                continue;
            }

            var candidateSchedule = KeepPlacementOrder(schedule, assignment.Schedule);
            var candidateScore = calculator.Evaluate(candidateSchedule).Value;
            if (candidateScore < bestScore - ScoreTolerance)
            {
                bestScore = candidateScore;
                bestSchedule = candidateSchedule;
            }
        }

        return bestSchedule;
    }

    // Thứ tự A: theo mốc bắt đầu (mốc tăng dần qua các ngày), cùng mốc thì theo thứ tự nhập.
    public static IReadOnlyList<int> OrderByStart(SchedulingProblem problem, Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);

        // Có thể sort theo slotIndex nhưng trong c# muốn sort thì hơi rắc rối, nên for theo slotIndex rồi lấy sessionId.
        var sessionIds = new List<int>();
        foreach (var slot in problem.Timeline.Slots)
        {
            foreach (var session in problem.Sessions)
            {
                if (schedule.TryGetPlacement(session.Id, out var placement) && placement.SlotIndex == slot.Index)
                {
                    sessionIds.Add(session.Id);
                }
            }
        }

        return sessionIds;
    }

    // Thứ tự B: phiên đông người chọn phòng trước, bằng nhau thì theo thứ tự nhập.
    public static IReadOnlyList<int> OrderBySizeDescending(SchedulingProblem problem, Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);

        var sessionIds = new List<int>();
        var initialPositions = new Dictionary<int, int>();
        foreach (var session in problem.Sessions)
        {
            if (schedule.IsPlaced(session.Id))
            {
                initialPositions[session.Id] = sessionIds.Count;
                sessionIds.Add(session.Id);
            }
        }

        sessionIds.Sort((firstId, secondId) =>
        {
            var sizeComparison = problem.GetSessionSize(secondId).CompareTo(problem.GetSessionSize(firstId));
            if (sizeComparison != 0)
            {
                return sizeComparison;
            }

            return initialPositions[firstId].CompareTo(initialPositions[secondId]);
        });

        return sessionIds;
    }

    // Helper của TryImprove (cùng lớp): tạo lịch mới có cùng thứ tự các phiên như lịch gốc, nhưng với phòng được gán lại từ assignedSchedule.
    private static Schedule KeepPlacementOrder(Schedule originalSchedule, Schedule assignedSchedule)
    {
        var schedule = new Schedule();
        foreach (var sessionId in originalSchedule.PlacedSessionIds)
        {
            assignedSchedule.TryGetPlacement(sessionId, out var placement);
            schedule.Place(sessionId, placement.SlotIndex, placement.RoomId!.Value);
        }

        return schedule;
    }
}
