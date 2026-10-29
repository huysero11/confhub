using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Constraints;

// Kiểm tra ràng buộc cứng H1-H5.
// CheckPlacement: thuật toán hỏi "đặt phiên này vào đây có được không" trước khi đặt.
// Validate: kiểm cả lịch, viết riêng và đơn giản để làm "trọng tài" cho test.
public sealed class ConstraintChecker
{
    private readonly SchedulingProblem _problem;

    // Dùng ở: T2.2 / T2.3 (tạo một lần cho mỗi bài toán), test.
    // Dùng ở: GreedyScheduler, FirstFitScheduler (constructor tạo bộ kiểm cho mỗi bài toán).
    // Dùng ở: LocalSearch, SimulatedAnnealing (constructor tạo bộ lọc cho các phép biến đổi).
    public ConstraintChecker(SchedulingProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _problem = problem;
    }

    // ---------- KIỂM MỘT VỊ TRÍ DỰ ĐỊNH ----------
    // Kiểm việc đặt phiên sessionId vào (slotIndex, roomId) cùng với phần còn lại của lịch.
    // Cần thời gian bắt đầu và kết thúc nên phải có PlacedSession do Placement chỉ lưu SlotIndex và RoomId
    // Dùng ở: T2.2 / T2.3 trước mỗi lần đặt / dời phiên; test (RandomScheduleBuilder).
    // Dùng ở: GreedyScheduler.TryPlaceSession, FirstFitScheduler.TryPlaceSession (lọc H1, H2, H5 trước khi tìm phòng).
    // Dùng ở: NeighborhoodMoves.TryMove / TrySwap (lọc vị trí mới trước khi tính f).
    public IReadOnlyList<ConstraintViolation> CheckPlacement(
        Schedule schedule,
        int sessionId,
        int slotIndex,
        int? roomId)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var violations = new List<ConstraintViolation>();

        // H5: vị trí phải thuộc danh sách mốc bắt đầu hợp lệ của phiên.
        if (!_problem.IsValidStartSlot(sessionId, slotIndex))
        {
            violations.Add(CreateInvalidSlotViolation(sessionId));
        }

        // Mốc không tồn tại thì không có khoảng thời gian để so với phiên khác.
        if (!IsSlotInRange(slotIndex))
        {
            return violations;
        }

        var candidate = BuildPlacedSession(sessionId, slotIndex, roomId);
        var others = BuildPlacedSessions(schedule, sessionId);

        // H1, H3: so với từng phiên đã đặt có khoảng thời gian giao nhau.
        foreach (var other in others)
        {
            if (!IsTimeOverlapping(candidate, other))
            {
                continue;
            }

            AddSpeakerViolation(violations, candidate.SessionId, other.SessionId);
            AddRoomClashViolation(violations, candidate, other);
        }

        // H2: tại mỗi mốc trong khoảng thời gian của phiên, số phiên đang diễn ra không vượt số phòng.
        AddCandidateConcurrencyViolations(violations, candidate, slotIndex, others);

        // H4: phòng phải đủ chỗ.
        if (roomId.HasValue)
        {
            AddCapacityViolation(violations, sessionId, roomId.Value);
        }

        return violations;
    }

    // ---------- KIỂM CẢ LỊCH ----------
    // Viết độc lập với CheckPlacement: duyệt từng phiên, từng cặp phiên, từng mốc.
    // Dùng ở: test (trọng tài cho mọi thuật toán); T2.4 kiểm lịch kết quả trước khi ghi chỉ số.
    // Dùng ở: LocalSearch.ValidateStart, SimulatedAnnealing.ValidateStart (từ chối lịch đầu vi phạm).
    public IReadOnlyList<ConstraintViolation> Validate(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var violations = new List<ConstraintViolation>();
        var placedSessions = new List<PlacedSession>();

        // H5 và H4 kiểm theo từng phiên.
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            if (!schedule.TryGetPlacement(sessionId, out var placement))
            {
                continue;
            }

            if (!_problem.IsValidStartSlot(sessionId, placement.SlotIndex))
            {
                violations.Add(CreateInvalidSlotViolation(sessionId));
            }

            if (IsSlotInRange(placement.SlotIndex))
            {
                placedSessions.Add(BuildPlacedSession(sessionId, placement.SlotIndex, placement.RoomId));

                if (placement.RoomId.HasValue)
                {
                    AddCapacityViolation(violations, sessionId, placement.RoomId.Value);
                }
            }
        }

        // H1 và H3 kiểm theo từng cặp phiên chồng giờ.
        for (var first = 0; first < placedSessions.Count; first++)
        {
            for (var second = first + 1; second < placedSessions.Count; second++)
            {
                if (IsTimeOverlapping(placedSessions[first], placedSessions[second]))
                {
                    AddSpeakerViolation(violations, placedSessions[first].SessionId, placedSessions[second].SessionId);
                    AddRoomClashViolation(violations, placedSessions[first], placedSessions[second]);
                }
            }
        }

        // H2 kiểm theo từng mốc: mỗi mốc vi phạm báo một lần.
        foreach (var slot in _problem.Timeline.Slots)
        {
            var runningSessionIds = FindRunningSessionIds(placedSessions, slot.DayIndex, slot.StartMinute);
            if (runningSessionIds.Count > _problem.Rooms.Count)
            {
                violations.Add(CreateTooManySessionsViolation(runningSessionIds, slot.StartMinute));
            }
        }

        return violations;
    }

    // ---------- HÀM TĨNH ----------
    // Helper của CheckPlacement và Validate (cùng lớp): hai phiên có chồng giờ không (phải cùng ngày).
    private static bool IsTimeOverlapping(PlacedSession first, PlacedSession second)
    {
        return first.DayIndex == second.DayIndex && first.Interval.Overlaps(second.Interval);
    }

    // Các phiên đang diễn ra tại phút minute của ngày dayIndex.
    // Helper của Validate và AddCandidateConcurrencyViolations (cùng lớp), dùng cho H2.
    private static List<int> FindRunningSessionIds(List<PlacedSession> placedSessions, int dayIndex, int minute)
    {
        var runningSessionIds = new List<int>();

        foreach (var placed in placedSessions)
        {
            var isRunning = placed.DayIndex == dayIndex
                && placed.Interval.StartMinute <= minute
                && minute < placed.Interval.EndMinute;

            if (isRunning)
            {
                runningSessionIds.Add(placed.SessionId);
            }
        }

        return runningSessionIds;
    }

    // Helper của CheckPlacement và Validate (cùng lớp): tạo vi phạm H5.
    private static ConstraintViolation CreateInvalidSlotViolation(int sessionId)
    {
        return new ConstraintViolation(
            ConstraintCode.H5,
            [sessionId],
            $"Phiên {sessionId} không đặt được ở mốc này (ngoài cửa sổ ngày, qua khung nghỉ hoặc diễn giả không rảnh).");
    }

    // Helper của Validate và AddCandidateConcurrencyViolations (cùng lớp): tạo vi phạm H2.
    private static ConstraintViolation CreateTooManySessionsViolation(List<int> runningSessionIds, int minute)
    {
        return new ConstraintViolation(
            ConstraintCode.H2,
            runningSessionIds,
            $"Lúc {minute} phút có {runningSessionIds.Count} phiên cùng diễn ra, nhiều hơn số phòng.");
    }

    // H3: hai phiên chồng giờ cùng một phòng (phiên chưa có phòng thì bỏ qua).
    // Helper của CheckPlacement và Validate (cùng lớp).
    private static void AddRoomClashViolation(List<ConstraintViolation> violations, PlacedSession first, PlacedSession second)
    {
        if (first.RoomId.HasValue && first.RoomId == second.RoomId)
        {
            violations.Add(new ConstraintViolation(
                ConstraintCode.H3,
                [first.SessionId, second.SessionId],
                $"Phiên {first.SessionId} và {second.SessionId} chồng giờ nhưng cùng phòng {first.RoomId.Value}."));
        }
    }

    // ---------- HÀM DÙNG DỮ LIỆU BÀI TOÁN ----------
    // Helper của CheckPlacement, Validate, BuildPlacedSessions (cùng lớp): mốc có tồn tại trong T không.
    private bool IsSlotInRange(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _problem.Timeline.Slots.Count;
    }

    // Helper của CheckPlacement, Validate, BuildPlacedSessions (cùng lớp): đổi (phiên, mốc, phòng)
    // thành PlacedSession có khoảng thời gian, để so chồng giờ.
    private PlacedSession BuildPlacedSession(int sessionId, int slotIndex, int? roomId)
    {
        var (dayIndex, interval) = _problem.GetSessionInterval(sessionId, slotIndex);
        return new PlacedSession(sessionId, dayIndex, interval, roomId);
    }

    // Các phiên đã đặt trong lịch (trừ phiên excludedSessionId), bỏ phiên có mốc không tồn tại.
    // Helper của CheckPlacement (cùng lớp).
    private List<PlacedSession> BuildPlacedSessions(Schedule schedule, int excludedSessionId)
    {
        var placedSessions = new List<PlacedSession>();

        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            if (sessionId == excludedSessionId)
            {
                continue;
            }

            if (schedule.TryGetPlacement(sessionId, out var placement) && IsSlotInRange(placement.SlotIndex))
            {
                placedSessions.Add(BuildPlacedSession(sessionId, placement.SlotIndex, placement.RoomId));
            }
        }

        return placedSessions;
    }

    // H1: hai phiên chồng giờ có chung một diễn giả.
    // Helper của CheckPlacement và Validate (cùng lớp).
    private void AddSpeakerViolation(List<ConstraintViolation> violations, int firstSessionId, int secondSessionId)
    {
        var firstSession = _problem.GetSession(firstSessionId);
        var secondSession = _problem.GetSession(secondSessionId);

        foreach (var speakerId in firstSession.SpeakerIds)
        {
            if (secondSession.SpeakerIds.Contains(speakerId))
            {
                violations.Add(new ConstraintViolation(
                    ConstraintCode.H1,
                    [firstSessionId, secondSessionId],
                    $"Phiên {firstSessionId} và {secondSessionId} chồng giờ nhưng cùng có diễn giả {speakerId}."));
            }
        }
    }

    // H4: sức chứa phòng phải đủ cho quy mô phiên.
    // Helper của CheckPlacement và Validate (cùng lớp).
    private void AddCapacityViolation(List<ConstraintViolation> violations, int sessionId, int roomId)
    {
        var room = _problem.GetRoom(roomId);
        var size = _problem.GetSessionSize(sessionId);

        if (room.Capacity < size)
        {
            violations.Add(new ConstraintViolation(
                ConstraintCode.H4,
                [sessionId],
                $"Phòng {roomId} chứa {room.Capacity} chỗ, ít hơn quy mô {size} của phiên {sessionId}."));
        }
    }

    // H2 cho phiên dự định: xét từng mốc nằm trong khoảng thời gian của phiên đó.
    // Helper của CheckPlacement (cùng lớp).
    private void AddCandidateConcurrencyViolations(
        List<ConstraintViolation> violations,
        PlacedSession candidate,
        int slotIndex,
        List<PlacedSession> others)
    {
        var allSessions = new List<PlacedSession>(others) { candidate };
        var slots = _problem.Timeline.Slots;

        for (var index = slotIndex; index < slots.Count; index++)
        {
            var slot = slots[index];
            if (slot.DayIndex != candidate.DayIndex || slot.StartMinute >= candidate.Interval.EndMinute)
            {
                break;
            }

            var runningSessionIds = FindRunningSessionIds(allSessions, slot.DayIndex, slot.StartMinute);
            if (runningSessionIds.Count > _problem.Rooms.Count)
            {
                violations.Add(CreateTooManySessionsViolation(runningSessionIds, slot.StartMinute));
            }
        }
    }
}
