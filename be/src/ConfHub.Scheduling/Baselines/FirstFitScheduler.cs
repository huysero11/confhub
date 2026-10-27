using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Baselines;

// M1 mô phỏng xếp thủ công: thứ tự nhập, mốc đầu tiên và phòng đầu tiên phù hợp.
// Không chấm f, không gán lại phòng của phiên cũ.
public sealed class FirstFitScheduler
{
    private readonly SchedulingProblem _problem;
    private readonly ConstraintChecker _checker;

    // Dùng ở: T2.4 và test (tạo phương pháp đối chứng M1 cho một bài toán).
    public FirstFitScheduler(SchedulingProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _problem = problem;
        _checker = new ConstraintChecker(problem);
    }

    // ---------- CHẠY M1 ----------
    // Dùng ở: T2.4 và test (xếp lịch M1, tầng gọi chấm điểm để so với tham lam).
    public SchedulingResult Run()
    {
        // Khác tham lam: giữ thứ tự nhập, không sắp theo độ ràng buộc
        var schedule = new Schedule();
        var unplacedSessions = new List<UnplacedSession>();
        foreach (var session in _problem.Sessions)
        {
            var blockedSlotCounts = new Dictionary<ConstraintCode, int>(); // đếm lỗi theo mã, giống GreedyScheduler
            if (!TryPlaceSession(schedule, session.Id, blockedSlotCounts))
            {
                unplacedSessions.Add(UnplacedSession.Create(session.Id, blockedSlotCounts));
            }
        }

        return new SchedulingResult(schedule, unplacedSessions);
    }

    // ---------- TÌM VỊ TRÍ ĐẦU TIÊN ----------
    // Helper của TryPlaceSession (cùng lớp): đếm mốc theo mã, không đếm lặp khi nhiều diễn giả cùng bận.
    private static void CountBlockedSlot(Dictionary<ConstraintCode, int> blockedSlotCounts, IReadOnlyList<ConstraintViolation> violations)
    {
        var constraintCodes = new HashSet<ConstraintCode>();
        foreach (var violation in violations)
        {
            constraintCodes.Add(violation.Code);
        }

        foreach (var constraintCode in constraintCodes)
        {
            // Tương đương blockedSlotCounts[constraintCode]++, mã chưa có thì coi là 0
            blockedSlotCounts.TryGetValue(constraintCode, out var blockedCount);
            blockedSlotCounts[constraintCode] = blockedCount + 1;
        }
    }

    // Helper của Run (cùng lớp): dừng ngay ở mốc và phòng đầu tiên qua mọi ràng buộc.
    private bool TryPlaceSession(Schedule schedule, int sessionId, Dictionary<ConstraintCode, int> blockedSlotCounts)
    {
        var validSlots = _problem.GetValidStartSlots(sessionId);
        if (validSlots.Count == 0)
        {
            blockedSlotCounts[ConstraintCode.H5] = _problem.Timeline.Slots.Count;
            return false;
        }

        var sessionSize = _problem.GetSessionSize(sessionId);
        foreach (var slotIndex in validSlots)
        {
            var violations = _checker.CheckPlacement(schedule, sessionId, slotIndex, null);
            if (violations.Count > 0)
            {
                CountBlockedSlot(blockedSlotCounts, violations);
                continue;
            }

            var (dayIndex, interval) = _problem.GetSessionInterval(sessionId, slotIndex);
            var freeRoomIds = RoomFinder.FindFreeRoomIds(_problem, schedule, dayIndex, interval, []);
            foreach (var roomId in freeRoomIds)
            {
                if (_problem.GetRoom(roomId).Capacity >= sessionSize)
                {
                    schedule.Place(sessionId, slotIndex, roomId);
                    return true;
                }
            }

            var failureCode = ConstraintCode.H4;
            if (freeRoomIds.Count == 0)
            {
                failureCode = ConstraintCode.H2;
            }

            blockedSlotCounts.TryGetValue(failureCode, out var blockedCount);
            blockedSlotCounts[failureCode] = blockedCount + 1;
        }

        return false;
    }
}
