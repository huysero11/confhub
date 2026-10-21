namespace ConfHub.Scheduling.Problem;

// Tập mốc thời gian T của bài toán.
// Mỗi ngày chia thành các mốc dài slotMinutes; mốc nào chạm khung nghỉ thì bị loại.
public sealed class Timeline
{
    private readonly List<TimeSlot> _slots = [];
    private readonly int _slotMinutes;

    // Dùng ở: constructor SchedulingProblem; ProblemGenerator.CheckLoad (đếm số phút dùng được).
    public Timeline(IReadOnlyList<SchedulingDay> days, int slotMinutes)
    {
        _slotMinutes = slotMinutes;

        for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
        {
            AddDaySlot(days[dayIndex], dayIndex);
        }
    }

    // Tập mốc T. Dùng ở: SchedulingProblem (GetSessionInterval, BuildValidStartSlots),
    // ConstraintChecker (H2, kiểm mốc tồn tại), ObjectiveCalculator (B), ProblemGenerator.CheckLoad.
    public IReadOnlyList<TimeSlot> Slots => _slots;

    // Dùng ở: SchedulingProblem.BuildValidStartSlots (phần "trong cửa sổ ngày, không qua khung nghỉ" của H5).
    /*
        Kiểm tra một session kéo dài durationMinutes có thể bắt đầu ở slotIndex hay không
        - Gọi endMinute là thời điểm kết thức phiên đang cần kiểm tra
        - Duyệt các mốc trong _slots, với 2 cái liền nhau là i và i - 1:
            + Cùng ngày, tức là _slots[i - 1].DayIndex = _slots[i].DayIndex
            + Chúng liên tục, tức là _slots[i - 1].StartMinute + _slotMinutes = _slots[i].StartMinute
            + Thỏa mãn khi thời điểm kết thúc _slots[i] >= endMinute,
                tức là _slots[i].StartMinute + _slotMinutes >= endMinute
    */
    public bool CanHost(int slotIndex, int durationMinutes)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return false;
        }

        var slotInterval = _slots[slotIndex];
        var startMinute = slotInterval.StartMinute;
        var endMinute = startMinute + durationMinutes;

        // Nếu bắt đầu trước slot đầu tiên hoặc kết thúc sau slot cuối cùng thì sai (không tồn tại)
        if (startMinute < _slots[0].StartMinute || endMinute > _slots[_slots.Count - 1].StartMinute + _slotMinutes)
        {
            return false;
        }

        for (var i = slotIndex; i < _slots.Count; i++)
        {
            if (i > slotIndex)
            {
                // Ko cùng ngày
                if (_slots[i - 1].DayIndex != _slots[i].DayIndex)
                {
                    return false;
                }

                // Ko liên tục
                if (_slots[i - 1].StartMinute + _slotMinutes != _slots[i].StartMinute)
                {
                    return false;
                }
            }

            if (_slots[i].StartMinute + _slotMinutes >= endMinute)
            {
                return true;
            }
        }

        return false;
    }

    // Helper của constructor (cùng lớp): sinh các mốc của một ngày.
    /* Thêm vào _slot những mốc thời gian có thể bắt đầu session, không chạm vào khung giờ nghỉ
        Duyệt từng mốc thời gian, mỗi mốc kiểm tra xem có trùng khoảng nghỉ nào không
    */
    private void AddDaySlot(SchedulingDay day, int dayIndex)
    {
        for (var start = day.StartMinute; start + _slotMinutes <= day.EndMinute; start += _slotMinutes)
        {
            var slotInterval = new TimeInterval(start, start + _slotMinutes);
            var touchesBreak = false;
            foreach (var dayBreak in day.Breaks)
            {
                if (slotInterval.Overlaps(dayBreak))
                {
                    touchesBreak = true;
                    break;
                }
            }

            if (!touchesBreak)
            {
                _slots.Add(new TimeSlot(_slots.Count, dayIndex, start));
            }
        }
    }
}
