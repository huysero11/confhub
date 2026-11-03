using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Objective;

// So vị trí với lịch đã công bố, chỉ tính phiên còn tồn tại trong bài toán mới.
// Giữ bản sao lịch gốc để việc tầng gọi sửa lịch sau đó không làm thay đổi mốc so sánh.
public sealed class DisruptionPenalty
{
    private readonly Schedule _baseSchedule;
    private readonly List<int> _baseSessionIds = [];

    // Dùng ở: Rescheduler.Run, test và chương trình đo (dựng mốc so sánh xáo trộn).
    public DisruptionPenalty(SchedulingProblem problem, Schedule baseSchedule, double weight)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(baseSchedule);
        if (!double.IsFinite(weight) || weight < 0)
        {
            throw new ArgumentException("Trọng số xáo trộn phải hữu hạn và không âm.", nameof(weight));
        }

        Weight = weight;
        _baseSchedule = baseSchedule.Clone();
        foreach (var session in problem.Sessions)
        {
            if (baseSchedule.IsPlaced(session.Id))
            {
                _baseSessionIds.Add(session.Id);
            }
        }
    }

    public int BaseSessionCount => _baseSessionIds.Count;

    public double Weight { get; }

    // ---------- ĐẾM VÀ LIỆT KÊ XÁO TRỘN ----------
    // Dùng ở: ObjectiveCalculator.Evaluate, chương trình đo (đếm D, không tính phiên mới hoặc đã xóa).
    public int CountDisrupted(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var disruptedCount = 0;
        foreach (var sessionId in _baseSessionIds)
        {
            if (IsDisrupted(schedule, sessionId))
            {
                disruptedCount++;
            }
        }

        return disruptedCount;
    }

    // Dùng ở: Rescheduler.Run (trả mã phiên bị xáo trộn theo thứ tự nhập của bài toán mới).
    public IReadOnlyList<int> GetDisruptedSessionIds(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var disruptedSessionIds = new List<int>();
        foreach (var sessionId in _baseSessionIds)
        {
            if (IsDisrupted(schedule, sessionId))
            {
                disruptedSessionIds.Add(sessionId);
            }
        }

        return disruptedSessionIds;
    }

    // Helper của CountDisrupted / GetDisruptedSessionIds (cùng lớp): thiếu phiên hoặc đổi mốc / phòng tính một lần.
    private bool IsDisrupted(Schedule schedule, int sessionId)
    {
        _baseSchedule.TryGetPlacement(sessionId, out var basePlacement);
        return !schedule.TryGetPlacement(sessionId, out var placement) || placement != basePlacement;
    }
}
