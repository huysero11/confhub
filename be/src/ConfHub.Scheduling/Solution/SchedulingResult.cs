namespace ConfHub.Scheduling.Solution;

// Kết quả chung của tham lam, M1 và M2; tầng gọi tự chấm điểm bằng cùng trọng số.
public sealed record SchedulingResult(Schedule Schedule, IReadOnlyList<UnplacedSession> UnplacedSessions);
