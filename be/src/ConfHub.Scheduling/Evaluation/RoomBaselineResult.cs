// Kết quả gán phòng tham lam; lịch chỉ chứa các phiên gán được phòng.
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Evaluation;

public sealed record RoomBaselineResult(
    Schedule Schedule,
    int WastedSeats,
    IReadOnlyList<int> FailedSessionIds);
