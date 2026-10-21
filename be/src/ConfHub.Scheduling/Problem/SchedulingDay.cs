namespace ConfHub.Scheduling.Problem;

// Một ngày hội nghị: cửa sổ làm việc [StartMinute, EndMinute) và các khung nghỉ trong ngày.
// 8:00 - 12:00, nghỉ 10:00 - 10:30
public sealed record SchedulingDay(int StartMinute, int EndMinute, IReadOnlyList<TimeInterval> Breaks);
