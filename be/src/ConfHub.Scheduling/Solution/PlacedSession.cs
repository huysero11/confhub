using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Solution;

// Một phiên đã đặt cùng khoảng thời gian nó chiếm; dùng nội bộ để so cặp phiên (ràng buộc, hàm mục tiêu).
internal readonly record struct PlacedSession(int SessionId, int DayIndex, TimeInterval Interval, int? RoomId);
