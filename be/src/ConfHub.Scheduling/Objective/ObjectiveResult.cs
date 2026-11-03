namespace ConfHub.Scheduling.Objective;

// Kết quả đánh giá một lịch. Ba số thô khớp các cột ConflictCount, LoadStdDev, WastedSeats,
// Value là f, hoặc f' khi có phạt; hai chỉ số xáo trộn bằng 0 nếu không truyền phạt.
public sealed record ObjectiveResult(
    int ConflictCount,
    double LoadStdDev,
    int WastedSeats,
    double NormalizedConflict,
    double NormalizedLoad,
    double NormalizedWaste,
    double Value,
    int DisruptedCount = 0,
    double NormalizedDisruption = 0);
