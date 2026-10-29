namespace ConfHub.Scheduling.Problem;

// Khoảng thời gian nửa mở [StartMinute, EndMinute) trong một ngày, tính bằng phút kể từ 0:00.
// new TimeInterval(480, 720); // 8:00 - 12:00
public readonly record struct TimeInterval(int StartMinute, int EndMinute)
{
    // Độ dài khoảng (phút). Hiện chỉ test dùng; để sẵn cho thống kê ở T2.4.
    public int Length => EndMinute - StartMinute;

    // Hai khoảng có giao nhau không (chạm đầu mút không tính là giao).
    // Dùng ở: Timeline.AddDaySlot (mốc chạm khung nghỉ), ConstraintChecker.IsTimeOverlapping (H1, H3),
    // ObjectiveCalculator.CalculateConflictCount (C).
    // Dùng ở: RoomFinder.FindFreeRoomIds (loại phòng bị chiếm trong bất kỳ phần nào của khoảng).
    // Dùng ở: ConflictContribution.Calculate, NeighborhoodMoves.TrySwap, SimulatedAnnealing.TryRandomSwap (xác định cặp chồng giờ).
    public bool Overlaps(TimeInterval other)
    {
        return StartMinute < other.EndMinute && EndMinute > other.StartMinute;
    }

    // Khoảng này có chứa trọn khoảng kia không.
    // Dùng ở: SchedulingProblem.AreAllSpeakersFree (phiên phải nằm trọn trong giờ rảnh - H5).
    public bool Contains(TimeInterval other)
    {
        return StartMinute <= other.StartMinute && other.EndMinute <= EndMinute;
    }
}
