// Kết quả gán phòng theo một thứ tự: lịch chỉ chứa các phiên gán được phòng.
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Rooms;

public sealed record RoomBaselineResult(
    Schedule Schedule,
    int WastedSeats,
    IReadOnlyList<int> FailedSessionIds);
