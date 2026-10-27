using ConfHub.Scheduling.Constraints;

namespace ConfHub.Scheduling.Rooms;

// Kết quả phân công phòng cho cả nhóm cùng khoảng thời gian, gồm cả phiên đang thử.
// GroupRoomAssigner chỉ đề xuất; thuật toán chọn mốc mới áp dụng vào lịch.
// RoomBySession = map lưu {sessionId, roomId}
public sealed record RoomAssignment(
    bool IsFeasible,
    ConstraintCode? FailureCode,
    IReadOnlyDictionary<int, int> RoomBySession);
