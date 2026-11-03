using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Greedy;

// Giai đoạn 1: duyệt phiên theo độ ràng buộc, chọn mốc cho f nhỏ nhất sau khi gán phòng.
// Run tạo lịch mới; Complete xếp tiếp trên bản sao, có thể phạt xáo trộn và giữ phiên ghim.
public sealed class GreedyScheduler
{
    private const double ScoreTolerance = 1e-12;
    private readonly SchedulingProblem _problem;
    private readonly ConstraintChecker _checker;
    private readonly ObjectiveCalculator _calculator;

    public GreedyScheduler(SchedulingProblem problem, ObjectiveWeights weights, DisruptionPenalty? disruption = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(weights);

        _problem = problem;
        _checker = new ConstraintChecker(problem);
        _calculator = new ObjectiveCalculator(problem, weights, disruption);
    }

    // ---------- ĐIỀU PHỐI XẾP PHIÊN ----------
    // Xếp cả lịch từ lịch rỗng theo thứ tự truyền vào (mặc định theo độ ràng buộc).
    public SchedulingResult Run(IReadOnlyList<int>? sessionOrder = null)
    {
        var orderedSessionIds = sessionOrder ?? ConstraintDegree.OrderSessions(_problem);
        ValidateSessionOrder(orderedSessionIds);

        return Complete(new Schedule(), orderedSessionIds);
    }

    // Xếp tiếp đúng danh sách phiên (theo thứ tự đó) lên bản sao của lịch có sẵn.
    public SchedulingResult Complete(Schedule start, IReadOnlyList<int> sessionIds, IReadOnlySet<int>? lockedSessionIds = null)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(sessionIds);
        ValidateCompletion(start, sessionIds);

        var schedule = start.Clone();
        var unplacedSessions = new List<UnplacedSession>();
        foreach (var sessionId in sessionIds)
        {
            var blockedSlotCounts = new Dictionary<ConstraintCode, int>(); // đếm lỗi theo mã {H5, 4} là có 4 slot bị lỗi H5
            if (!TryPlaceSession(schedule, sessionId, blockedSlotCounts, lockedSessionIds))
            {
                unplacedSessions.Add(UnplacedSession.Create(sessionId, blockedSlotCounts));
            }
        }

        return new SchedulingResult(schedule, unplacedSessions);
    }

    // ---------- GÁN PHÒNG VÀ GHI LÝ DO ----------
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

    // Helper của TryPlaceSession (cùng lớp): áp dụng mốc mới và phòng của cả nhóm.
    private static void ApplyAssignment(Schedule schedule, int sessionId, int slotIndex, RoomAssignment roomAssignment)
    {
        schedule.Place(sessionId, slotIndex);
        foreach (var sessionRoom in roomAssignment.RoomBySession)
        {
            // sessionRoom = {sessionId, roomId}
            schedule.SetRoom(sessionRoom.Key, sessionRoom.Value);
        }
    }

    // ---------- KIỂM DANH SÁCH PHIÊN ----------
    // Helper của Run (cùng lớp): thứ tự phải chứa mọi phiên đúng một lần.
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

    // Helper của Complete (cùng lớp): chỉ nhận mã tồn tại, chưa đặt và không trùng trong danh sách.
    private void ValidateCompletion(Schedule start, IReadOnlyList<int> sessionIds)
    {
        var seenSessionIds = new HashSet<int>();
        foreach (var sessionId in sessionIds)
        {
            _problem.GetSession(sessionId);
            if (!seenSessionIds.Add(sessionId) || start.IsPlaced(sessionId))
            {
                throw new ArgumentException("Phiên bị trùng hoặc đã có trong lịch đầu.", nameof(sessionIds));
            }
        }
    }

    // ---------- CHỌN MỐC TỐT NHẤT ----------
    // Helper của Complete (cùng lớp): thử các mốc, chấm f hoặc f' sau khi gán phòng.
    private bool TryPlaceSession(Schedule schedule, int sessionId, Dictionary<ConstraintCode, int> blockedSlotCounts, IReadOnlySet<int>? lockedSessionIds)
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
            var assignment = GroupRoomAssigner.TryAssign(_problem, schedule, sessionId, slotIndex, lockedSessionIds);
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
