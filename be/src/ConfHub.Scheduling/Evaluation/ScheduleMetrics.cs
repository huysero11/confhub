// Các chỉ số đánh giá một lịch, giữ cả số thô và tỷ lệ để xuất thực nghiệm.
namespace ConfHub.Scheduling.Evaluation;

public sealed record ScheduleMetrics(
    int SessionCount,
    int PlacedCount,
    double PlacementRate,
    int ConflictCount,
    double LoadStdDev,
    int WastedSeats,
    double Objective,
    double NoLossAttendeeRate,
    double RoomFillRate,
    int ViolationCount);
