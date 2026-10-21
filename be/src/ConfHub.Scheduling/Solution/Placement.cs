namespace ConfHub.Scheduling.Solution;

// Vị trí của một phiên trong lịch: mốc bắt đầu và phòng (null nếu chưa gán phòng).
public readonly record struct Placement(int SlotIndex, int? RoomId);
