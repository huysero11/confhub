using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rescheduling;

// Kết quả xếp lại cho tầng gọi: lịch mới, phiên bị gỡ, chưa xếp, xáo trộn và điểm f'.
public sealed record RescheduleResult(
    Schedule Schedule,
    IReadOnlyList<UnplacedSession> UnplacedSessions,
    IReadOnlyList<DisplacedSession> DisplacedSessions,
    IReadOnlyList<int> ChangedSessionIds,
    double Value);
