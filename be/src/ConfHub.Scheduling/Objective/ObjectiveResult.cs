namespace ConfHub.Scheduling.Objective;

// Kết quả đánh giá một lịch. Ba số thô khớp các cột ConflictCount, LoadStdDev, WastedSeats,
// còn Value (f) khớp cột Objective của bảng ScheduleVersion.
public sealed record ObjectiveResult(
    int ConflictCount,
    double LoadStdDev,
    int WastedSeats,
    double NormalizedConflict,
    double NormalizedLoad,
    double NormalizedWaste,
    double Value);
