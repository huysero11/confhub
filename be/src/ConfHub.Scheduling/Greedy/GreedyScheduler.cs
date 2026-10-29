using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Greedy;

// Giai đoạn 1: duyệt phiên theo độ ràng buộc, chọn mốc cho f nhỏ nhất sau khi gán phòng.
// Mỗi lần Run tạo lịch mới; T2.3 có thể truyền thứ tự khác để khởi động lại.
public sealed class GreedyScheduler
{
    private const double ScoreTolerance = 1e-12;
    private readonly SchedulingProblem _problem;
    private readonly ConstraintChecker _checker;
    private readonly ObjectiveCalculator _calculator;

    // Dùng ở: InterestBlindScheduler.Run (M2); T2.3 / T2.4 và test (chạy tham lam với trọng số chọn trước).
    // Dùng ở: LocalSearchScheduler, RestartScheduler, AnnealingScheduler (constructor tạo bước tham lam đầu).
    public GreedyScheduler(SchedulingProblem problem, ObjectiveWeights weights)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(weights);

        _problem = problem;
        _checker = new ConstraintChecker(problem);
        _calculator = new ObjectiveCalculator(problem, weights);
    }

    // Dùng ở: LocalSearchScheduler.Run, RestartScheduler.Run, AnnealingScheduler.Run (dựng lịch trước khi cải thiện).
    public SchedulingResult Run(IReadOnlyList<int>? sessionOrder = null)
    {
        var orderedSessionIds = sessionOrder ?? ConstraintDegree.OrderSessions(_problem);
        ValidateSessionOrder(orderedSessionIds);

        // Bắt đầu thử đặt các session vào lịch theo thứ tự ràng buộc
        var schedule = new Schedule();
        var unplacedSessions = new List<UnplacedSession>();
        foreach (var sessionId in orderedSessionIds)
        {
            var blockedSlotCounts = new Dictionary<ConstraintCode, int>(); // đếm lỗi theo mã {H5, 4} là có 4 slot bị lỗi H5
            if (!TryPlaceSession(schedule, sessionId, blockedSlotCounts))
            {
                unplacedSessions.Add(UnplacedSession.Create(sessionId, blockedSlotCounts));
            }
        }

        return new SchedulingResult(schedule, unplacedSessions);
    }

    // Helper của TryPlaceSession (cùng lớp): một mã chỉ tính một lần ở mỗi mốc dù có nhiều vi phạm.
    private static void CountBlockedSlot(Dictionary<ConstraintCode, int> blockedSlotCounts, IReadOnlyList<ConstraintViolation> violations)
    {
        var constraintCodes = new HashSet<ConstraintCode>();
        foreach (var violation in violations)
        {
            constraintCodes.Add(violation.Code);
        }

        foreach (var constraintCode in constraintCodes)
        {
            // Tương đương blockedSlotCounts[constraintCode]++, nhưng mã chưa có trong từ điển thì coi là 0
            // (viết ++ thẳng sẽ ném KeyNotFoundException khi mã chưa có).
            blockedSlotCounts.TryGetValue(constraintCode, out var blockedCount);
            blockedSlotCounts[constraintCode] = blockedCount + 1;
        }
    }

    private static void ApplyAssignment(Schedule schedule, int sessionId, int slotIndex, RoomAssignment roomAssignment)
    {
        schedule.Place(sessionId, slotIndex);
        foreach (var sessionRoom in roomAssignment.RoomBySession)
        {
            // sessionRoom = {sessionId, roomId}
            schedule.SetRoom(sessionRoom.Key, sessionRoom.Value);
        }
    }

    // helper của Run
    /*
        - Validate mảng sessionOrder sau khi sắp xếp theo độ ràng buộc
        - Cách làm:
            - Cho mảng _problem.Sessions này vào set
            - Duyệt lại mảng này và xóa phần tử đó trong set => phải xóa được
    */
    private void ValidateSessionOrder(IReadOnlyList<int> sessionOrder)
    {
        if (sessionOrder.Count != _problem.Sessions.Count)
        {
            throw new ArgumentException("Thứ tự phải chứa đúng mọi phiên một lần.", nameof(sessionOrder));
        }

        var remainingSessionIds = new HashSet<int>();
        foreach (var session in _problem.Sessions)
        {
            remainingSessionIds.Add(session.Id);
        }

        // Duyệt sessionOrder (không phải tập vừa dựng): gạch từng mã, mã lạ hoặc đã gạch rồi thì Remove trả false
        foreach (var sessionId in sessionOrder)
        {
            if (!remainingSessionIds.Remove(sessionId))
            {
                throw new ArgumentException("Thứ tự có mã phiên lạ hoặc bị trùng.", nameof(sessionOrder));
            }
        }
    }

    private bool TryPlaceSession(Schedule schedule, int sessionId, Dictionary<ConstraintCode, int> blockedSlotCounts)
    {
        var validSlots = _problem.GetValidStartSlots(sessionId);
        if (validSlots.Count == 0)
        {
            // Mọi mốc trong T đều bị H5 chặn; T rỗng thì số mốc bằng 0, vẫn báo nguyên nhân H5.
            blockedSlotCounts[ConstraintCode.H5] = _problem.Timeline.Slots.Count;
            return false;
        }

        var bestSlotIndex = -1;
        var bestScore = double.PositiveInfinity;
        RoomAssignment? bestAssignment = null;
        foreach (var slotIndex in validSlots)
        {
            // Kiểm H1 / H2 / H5 nếu đặt phiên vào slotIndex (chỉ hỏi, chưa đặt thật, chưa có phòng).
            var violations = _checker.CheckPlacement(schedule, sessionId, slotIndex, null);
            if (violations.Count > 0)
            {
                CountBlockedSlot(blockedSlotCounts, violations);
                continue;
            }

            // Qua được thì thử chia phòng cho cả nhóm cùng khoảng (H3, H4); kết quả mới là đề xuất.
            var assignment = GroupRoomAssigner.TryAssign(_problem, schedule, sessionId, slotIndex);
            if (!assignment.IsFeasible)
            {
                var failureCode = assignment.FailureCode!.Value;
                blockedSlotCounts.TryGetValue(failureCode, out var blockedCount);
                blockedSlotCounts[failureCode] = blockedCount + 1;
                continue;
            }

            // Chấm f trên bản sao đã đặt thử phiên; lịch thật chưa bị đổi.
            var cloneSchedule = schedule.Clone();
            ApplyAssignment(cloneSchedule, sessionId, slotIndex, assignment);
            var candidateScore = _calculator.Evaluate(cloneSchedule).Value;

            // Mốc đã tăng dần; hòa trong sai số thì giữ mốc sớm đã chọn.
            if (candidateScore < bestScore - ScoreTolerance)
            {
                bestScore = candidateScore;
                bestAssignment = assignment;
                bestSlotIndex = slotIndex;
            }
        }

        if (bestAssignment is null)
        {
            return false;
        }

        ApplyAssignment(schedule, sessionId, bestSlotIndex, bestAssignment);
        return true;
    }
}
