namespace ConfHub.Scheduling.Problem;

// Một mốc thời gian của tập T: Index liên tục từ 0 qua mọi ngày, DayIndex đánh từ 0.
/*
Ngày 1 — DayIndex = 0
    8:00 (Index 0)      8:30 (Index 1)      9:00 (Index 2)      9:30 (Index 3)
    10:00 (break)      10:30 (Index 4)      11:00 (Index 5)      11:30 (Index 6)
Ngày 2 — DayIndex = 1
    8:00 (Index 7)      8:30 (Index 8)      9:00 (Index 9)
    9:30 (break)        10:00 (Index 10)      10:30 (Index 11)
*/
public readonly record struct TimeSlot(int Index, int DayIndex, int StartMinute);
