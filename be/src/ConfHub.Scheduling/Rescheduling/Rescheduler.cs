using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rescheduling;

// Xếp lại bốn bước: giữ phiên hợp lệ → tham lam xếp tiếp → nhường chỗ một bậc → leo đồi.
public sealed class Rescheduler
{
    private const double ScoreTolerance = 1e-12;
    private readonly SchedulingProblem _problem;
    private readonly RescheduleOptions _options;
    private readonly ObjectiveWeights _weights;
    private readonly ConstraintChecker _checker;

    // Dùng ở: test, chương trình đo và T5.4 (tạo bộ xếp lại cho bài toán đã chứa biến động).
    public Rescheduler(SchedulingProblem problem, RescheduleOptions? options = null, ObjectiveWeights? weights = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _problem = problem;
        _options = options ?? new RescheduleOptions();
        _options.Validate();
        _weights = weights ?? ObjectiveWeights.Default;
        _checker = new ConstraintChecker(problem);
    }

    // ---------- ĐIỀU PHỐI BỐN BƯỚC ----------
    // Dùng ở: test, chương trình đo và T5.4 (xếp lại từ lịch đã công bố, không sửa lịch gốc).
    public RescheduleResult Run(Schedule baseSchedule, IReadOnlySet<int>? lockedSessionIds = null)
    {
        ArgumentNullException.ThrowIfNull(baseSchedule);
        ValidateBaseSchedule(baseSchedule, lockedSessionIds);

        var disruption = new DisruptionPenalty(_problem, baseSchedule, _options.DisruptionWeight);
        var calculator = new ObjectiveCalculator(_problem, _weights, disruption);
        var greedyScheduler = new GreedyScheduler(_problem, _weights, disruption);

        // Bước 1: ghim hợp lệ được giữ trước để phiên không ghim không chiếm vị trí của chúng.
        var keptSchedule = new Schedule();
        var displacedSessions = new List<DisplacedSession>();
        var activeLockedSessionIds = new HashSet<int>();
        KeepValidSessions(baseSchedule, lockedSessionIds, true, keptSchedule, displacedSessions, activeLockedSessionIds);
        KeepValidSessions(baseSchedule, lockedSessionIds, false, keptSchedule, displacedSessions, activeLockedSessionIds);

        // Bước 2: chỉ xếp các phiên còn thiếu, đúng thứ tự độ ràng buộc.
        // Ví dụ A B C D bỏ B thì pendingSessionIds = {B}, nhưng không ghép vào bước 1 vì bài toán mới có thể THÊM phiên, bước 1 chỉ bỏ phiên không hợp lệ.
        var pendingSessionIds = new List<int>();
        foreach (var sessionId in ConstraintDegree.OrderSessions(_problem))
        {
            if (!keptSchedule.IsPlaced(sessionId))
            {
                pendingSessionIds.Add(sessionId);
            }
        }

        // Đặt các phiên pending đó vào lịch hiện tại.
        var greedyResult = greedyScheduler.Complete(keptSchedule, pendingSessionIds, activeLockedSessionIds);
        var currentSchedule = greedyResult.Schedule;
        var unplacedSessions = new List<UnplacedSession>();

        // Bước 3: mỗi phiên còn thiếu chỉ được nhờ một phiên khác nhường chỗ.
        foreach (var unplacedSession in greedyResult.UnplacedSessions)
        {
            var repairedSchedule = TryDisplace(currentSchedule, unplacedSession.SessionId, activeLockedSessionIds, greedyScheduler, calculator);
            if (repairedSchedule is null)
            {
                unplacedSessions.Add(unplacedSession);
            }
            else
            {
                currentSchedule = repairedSchedule;
            }
        }

        // Bước 4: chỉ nhận bước giảm f', giữ nguyên mốc và phòng của các phiên ghim còn hợp lệ.
        var improvement = new LocalSearch(_problem, _weights, _options.Improvement, disruption).Improve(currentSchedule, activeLockedSessionIds);
        return new RescheduleResult(
            improvement.Schedule,
            unplacedSessions,
            displacedSessions,
            disruption.GetDisruptedSessionIds(improvement.Schedule),
            improvement.Value);
    }

    // ---------- KIỂM VÀ GIỮ LỊCH GỐC ----------
    // Helper của Run (cùng lớp): mọi phiên đã xếp trong baseSchedule phải có RoomId,
    // mọi ID trong lockedSessionIds phải xuất hiện trong lịch gốc.
    private static void ValidateBaseSchedule(Schedule baseSchedule, IReadOnlySet<int>? lockedSessionIds)
    {
        foreach (var sessionId in baseSchedule.PlacedSessionIds)
        {
            baseSchedule.TryGetPlacement(sessionId, out var placement);
            if (!placement.RoomId.HasValue)
            {
                throw new ArgumentException("Phiên trong lịch gốc phải có phòng.", nameof(baseSchedule));
            }
        }

        if (lockedSessionIds is null)
        {
            return;
        }

        foreach (var sessionId in lockedSessionIds)
        {
            if (!baseSchedule.IsPlaced(sessionId))
            {
                throw new ArgumentException("Phiên ghim phải có trong lịch gốc.", nameof(lockedSessionIds));
            }
        }
    }

    // Helper của KeepValidSessions (cùng lớp): mã không lặp, xếp H1–H5 và lý do tiếng Việt ổn định.
    private static DisplacedSession BuildDisplacedSession(int sessionId, HashSet<ConstraintCode> violations)
    {
        var codes = new List<ConstraintCode>();
        var reasons = new List<string>();
        foreach (var code in Enum.GetValues<ConstraintCode>())
        {
            if (!violations.Contains(code))
            {
                continue;
            }

            codes.Add(code);
            reasons.Add(code switch
            {
                ConstraintCode.H1 => "trùng diễn giả (H1)",
                ConstraintCode.H2 => "hết phòng (H2)",
                ConstraintCode.H3 => "trùng phòng (H3)",
                ConstraintCode.H4 => "phòng không còn hoặc không đủ sức chứa (H4)",
                _ => "mốc không hợp lệ (H5)",
            });
        }

        return new DisplacedSession(sessionId, codes, $"Phiên {sessionId} phải dời: {string.Join(", ", reasons)}.");
    }

    /*
        Ví dụ có {A, B, C, D} mà B bị bỏ, thì hàm này sẽ cho ra kết quả sau
        - keptSchedule = {A, C, D}
        - displacedSessions = {B}

        keepLocked: true: xử lý phiên ghim; false: xử lý phiên không ghim

        Bài toán mới có thể đã mất phòng (ví dụ ChangeScenario.RemoveRoom ở test, hoặc BTC xóa phòng ở T5.4):
            problem không còn phòng đó nhưng lịch gốc vẫn ghi phòng cũ,
            nên phải so RoomId của lịch gốc với roomIds trước khi gọi CheckPlacement.
    */

    // Helper của Run (cùng lớp): duyệt từng nhóm ghim / không ghim theo thứ tự nhập.
    private void KeepValidSessions(
        Schedule baseSchedule,
        IReadOnlySet<int>? lockedSessionIds,
        bool keepLocked,
        Schedule keptSchedule,
        List<DisplacedSession> displacedSessions,
        HashSet<int> activeLockedSessionIds)
    {
        var roomIds = new HashSet<int>();
        foreach (var room in _problem.Rooms)
        {
            roomIds.Add(room.Id);
        }

        foreach (var session in _problem.Sessions)
        {
            var isLocked = lockedSessionIds?.Contains(session.Id) ?? false;

            // isLocked != keepLocked: Không thuộc nhóm đang xét → bỏ qua.
            // TryGetPlacement() thất bại: Phiên chưa có trong lịch cũ → bỏ qua.
            if (isLocked != keepLocked || !baseSchedule.TryGetPlacement(session.Id, out var placement))
            {
                continue;
            }

            // Kiểm tra phòng cũ có còn trong roomIds không, so RoomId từ schedule và roomIds từ problem.
            var codes = new HashSet<ConstraintCode>();
            if (!roomIds.Contains(placement.RoomId!.Value))
            {
                codes.Add(ConstraintCode.H4);
            }
            else
            {
                // Còn thì gọi CheckPlacement.
                var violations = _checker.CheckPlacement(keptSchedule, session.Id, placement.SlotIndex, placement.RoomId);
                foreach (var violation in violations)
                {
                    codes.Add(violation.Code);
                }
            }

            // Hết hợp lệ thì chỉ ghi vào danh sách gỡ, KHÔNG đặt vào keptSchedule (bước 2 sẽ xếp lại).
            if (codes.Count > 0)
            {
                displacedSessions.Add(BuildDisplacedSession(session.Id, codes));
                continue;
            }

            keptSchedule.Place(session.Id, placement.SlotIndex, placement.RoomId);
            if (isLocked)
            {
                activeLockedSessionIds.Add(session.Id);
            }
        }
    }

    // ---------- NHƯỜNG CHỖ MỘT BẬC ----------
    // Helper của Run (cùng lớp): lấy các phiên Y không bị ghim và chồng giờ với B, clone lịch, xóa Y, đặt B, tính điểm, nếu tốt hơn thì trả về lịch mới
    private Schedule? TryDisplace(
        Schedule schedule,
        int pendingSessionId,
        HashSet<int> lockedSessionIds,
        GreedyScheduler greedyScheduler,
        ObjectiveCalculator calculator)
    {
        Schedule? bestSchedule = null;
        var bestScore = double.PositiveInfinity;
        foreach (var slotIndex in _problem.GetValidStartSlots(pendingSessionId))
        {
            var (pendingDayIndex, pendingInterval) = _problem.GetSessionInterval(pendingSessionId, slotIndex);
            foreach (var otherSession in _problem.Sessions)
            {
                // Phiên bị ghim hoặc chưa đặt thì không xét.
                if (lockedSessionIds.Contains(otherSession.Id) || !schedule.TryGetPlacement(otherSession.Id, out var otherPlacement))
                {
                    continue;
                }

                // Phiên không chồng giờ thì không xét.
                var (otherDayIndex, otherInterval) = _problem.GetSessionInterval(otherSession.Id, otherPlacement.SlotIndex);
                if (pendingDayIndex != otherDayIndex || !pendingInterval.Overlaps(otherInterval))
                {
                    continue;
                }

                var candidateSchedule = schedule.Clone();
                candidateSchedule.Remove(otherSession.Id); // Xóa phiên Y

                // Đặt phiên B vào chỗ Y vừa trống.
                if (!TryPlaceDisplacedSession(candidateSchedule, pendingSessionId, slotIndex, lockedSessionIds))
                {
                    continue;
                }

                // Đặt phiên Y lại vào candidateSchedule; xếp không được cả hai thì bỏ cách này.
                var completion = greedyScheduler.Complete(candidateSchedule, [otherSession.Id], lockedSessionIds);
                if (!completion.Schedule.IsPlaced(otherSession.Id) || !completion.Schedule.IsPlaced(pendingSessionId))
                {
                    continue;
                }

                var candidateScore = calculator.Evaluate(completion.Schedule).Value;
                if (candidateScore < bestScore - ScoreTolerance)
                {
                    bestScore = candidateScore;
                    bestSchedule = completion.Schedule;
                }
            }
        }

        return bestSchedule;
    }

    // Helper của TryDisplace (cùng lớp): đặt phiên bị dời vào lịch, nếu hợp lệ thì trả về true, ngược lại false
    private bool TryPlaceDisplacedSession(
        Schedule schedule,
        int sessionId,
        int slotIndex,
        HashSet<int> lockedSessionIds)
    {
        if (_checker.CheckPlacement(schedule, sessionId, slotIndex, null).Count > 0)
        {
            return false;
        }

        var assignment = GroupRoomAssigner.TryAssign(_problem, schedule, sessionId, slotIndex, lockedSessionIds);
        if (!assignment.IsFeasible)
        {
            return false;
        }

        schedule.Place(sessionId, slotIndex);
        foreach (var sessionRoom in assignment.RoomBySession)
        {
            schedule.SetRoom(sessionRoom.Key, sessionRoom.Value);
        }

        return true;
    }
}
