// Mốc so sánh gán phòng (chỉ số 10): đi theo thời gian, chọn phòng nhỏ nhất đủ chỗ và không xét lại.
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Evaluation;

public static class GreedyRoomBaseline
{
    // Gán lại các phiên đã có mốc theo giờ bắt đầu; lịch đầu vào giữ nguyên kể cả khi có phiên gán thất bại.
    public static RoomBaselineResult Assign(SchedulingProblem problem, Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);
        return BestFitRoomAssigner.Assign(problem, schedule, RoomReassigner.OrderByStart(problem, schedule));
    }
}
