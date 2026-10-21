using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Objective;

// Tính hàm mục tiêu f cho một lịch (càng nhỏ càng tốt).
//   C = số xung đột nguyện vọng, B = độ lệch chuẩn tải theo mốc, S = chỗ ngồi lãng phí.
// Mỗi thành phần chia cho một cận trên cố định của bài toán (không phụ thuộc lịch đang xét)
// nên so sánh được giữa các lịch, kể cả lịch chưa xếp đủ phiên.
// Chỉ xét phiên đã đặt. Chưa tối ưu hiệu năng: mỗi lần tính lại toàn bộ.
public sealed class ObjectiveCalculator
{
    private readonly SchedulingProblem _problem;
    private readonly ObjectiveWeights _weights;
    private readonly int _maxWastedSeats;

    // Dùng ở: T2.2 / T2.3 (tạo một lần cho mỗi bài toán), T2.4, test.
    public ObjectiveCalculator(SchedulingProblem problem, ObjectiveWeights weights)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(weights);

        _problem = problem;
        _weights = weights;
        _maxWastedSeats = CalculateMaxWastedSeats();
    }

    // Tính C, B, S, các giá trị chuẩn hóa và f của một lịch.
    // Dùng ở: T2.2 / T2.3 so sánh lịch trước và sau biến đổi; T2.4 ghi chỉ số; T5.1 lưu vào ScheduleVersion.
    public ObjectiveResult Evaluate(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var placedSessions = BuildPlacedSessions(schedule);

        var conflictCount = CalculateConflictCount(placedSessions);
        var loadStdDev = CalculateLoadStdDev(placedSessions);
        var wastedSeats = CalculateWastedSeats(placedSessions);

        // Chuẩn hóa về [0, 1]; mẫu số bằng 0 thì thành phần đó bằng 0.
        var normalizedConflict = 0.0;
        if (_problem.InterestMatrix.TotalCoInterest > 0)
        {
            normalizedConflict = conflictCount / (double)_problem.InterestMatrix.TotalCoInterest;
        }

        // Nếu H2 được giữ, số phiên mỗi mốc nằm trong [0, số phòng]. Theo bất đẳng thức Popoviciu,
        // độ lệch chuẩn không thể vượt (max - min) / 2 = số phòng / 2, nên Bn nằm trong [0, 1].
        var maxLoadStdDev = _problem.Rooms.Count / 2.0;
        var normalizedLoad = Math.Min(1.0, loadStdDev / maxLoadStdDev);

        var normalizedWaste = 0.0;
        if (_maxWastedSeats > 0)
        {
            normalizedWaste = wastedSeats / (double)_maxWastedSeats;
        }

        var value = (_weights.Conflict * normalizedConflict)
            + (_weights.Load * normalizedLoad)
            + (_weights.Waste * normalizedWaste);

        return new ObjectiveResult(
            conflictCount,
            loadStdDev,
            wastedSeats,
            normalizedConflict,
            normalizedLoad,
            normalizedWaste,
            value);
    }

    // ---------- CẬN CHUẨN HÓA ----------
    // Smax = chỗ lãng phí nhiều nhất có thể: mọi phiên đều vào phòng lớn nhất.
    // Helper của constructor (cùng lớp), tính một lần.
    private int CalculateMaxWastedSeats()
    {
        var largestCapacity = 0;
        foreach (var room in _problem.Rooms)
        {
            largestCapacity = Math.Max(largestCapacity, room.Capacity);
        }

        var maxWastedSeats = 0;
        foreach (var session in _problem.Sessions)
        {
            maxWastedSeats += Math.Max(0, largestCapacity - _problem.GetSessionSize(session.Id));
        }

        return maxWastedSeats;
    }

    // ---------- TÍNH C, B, S ----------
    // Helper của Evaluate (cùng lớp): lấy các phiên đã đặt kèm khoảng thời gian.
    private List<PlacedSession> BuildPlacedSessions(Schedule schedule)
    {
        var placedSessions = new List<PlacedSession>();
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            if (schedule.TryGetPlacement(sessionId, out var placement))
            {
                var (dayIndex, interval) = _problem.GetSessionInterval(sessionId, placement.SlotIndex);
                var placedSession = new PlacedSession(sessionId, dayIndex, interval, placement.RoomId);
                placedSessions.Add(placedSession);
            }
        }

        return placedSessions;
    }

    // C: cộng W[i][j] của mọi cặp phiên đã đặt có khoảng thời gian giao nhau.
    // Helper của Evaluate (cùng lớp).
    private int CalculateConflictCount(List<PlacedSession> placedSessions)
    {
        var conflictCount = 0;
        for (var i = 0; i < placedSessions.Count; i++)
        {
            for (var j = i + 1; j < placedSessions.Count; j++)
            {
                var first = placedSessions[i];
                var second = placedSessions[j];
                if (first.DayIndex == second.DayIndex
                    && first.Interval.Overlaps(second.Interval))
                {
                    conflictCount += _problem.InterestMatrix.Get(first.SessionId, second.SessionId);
                }
            }
        }

        return conflictCount;
    }

    // B: độ lệch chuẩn tổng thể của "số phiên đang diễn ra" trên mọi mốc dùng được (kể cả mốc 0 phiên).
    // B = √( Σ (số phiên mỗi slot − m)² / số phiên )
    // Helper của Evaluate (cùng lớp).
    private double CalculateLoadStdDev(List<PlacedSession> placedSessions)
    {
        var slots = _problem.Timeline.Slots;
        if (slots.Count == 0)
        {
            return 0.0;
        }

        // loads[i] là số phiên đang chạy tại timeslot i
        var loads = new int[slots.Count];
        var totalLoad = 0;
        for (var i = 0; i < slots.Count; i++)
        {
            foreach (var placedSession in placedSessions)
            {
                if (slots[i].DayIndex == placedSession.DayIndex
                    && placedSession.Interval.StartMinute <= slots[i].StartMinute && slots[i].StartMinute < placedSession.Interval.EndMinute)
                {
                    loads[i]++;
                }
            }

            totalLoad += loads[i];
        }

        var mean = totalLoad / (double)slots.Count;
        var sumOfSquare = 0.0;
        foreach (var load in loads)
        {
            sumOfSquare += (load - mean) * (load - mean);
        }

        return Math.Sqrt(sumOfSquare / slots.Count);
    }

    // S: tổng (sức chứa - quy mô) của các phiên đã đặt VÀ đã có phòng, không để âm.
    // Helper của Evaluate (cùng lớp).
    private int CalculateWastedSeats(List<PlacedSession> placedSessions)
    {
        var wastedSeats = 0;

        foreach (var placed in placedSessions)
        {
            if (placed.RoomId.HasValue)
            {
                var capacity = _problem.GetRoom(placed.RoomId.Value).Capacity;
                wastedSeats += Math.Max(0, capacity - _problem.GetSessionSize(placed.SessionId));
            }
        }

        return wastedSeats;
    }
}
