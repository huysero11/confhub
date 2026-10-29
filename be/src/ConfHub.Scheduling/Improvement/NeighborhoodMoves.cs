using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rooms;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Improvement;

// Hai phép biến đổi dùng chung cho cục bộ và luyện kim; lịch đầu vào luôn được giữ nguyên.
// Dời gán lại phòng ở nhóm đích; hoán đổi giữ đúng phòng trao đổi, không chạy Hungarian.
public static class NeighborhoodMoves
{
    // ---------- DỜI MỘT PHIÊN ----------
    // Dùng ở: LocalSearch.Improve, SimulatedAnnealing.TryRandomMove (thử mốc khác và lọc H1–H5 trước khi chấm f).
    public static Schedule? TryMove(SchedulingProblem problem, ConstraintChecker checker, Schedule schedule, int sessionId, int slotIndex)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(checker);
        ArgumentNullException.ThrowIfNull(schedule);

        // Phiên chưa đặt thì không có gì để dời; dời về chính mốc cũ không phải bước mới.
        if (!schedule.TryGetPlacement(sessionId, out var placement) || placement.SlotIndex == slotIndex)
        {
            return null;
        }

        // Gỡ phiên khỏi bản sao để GroupRoomAssigner không coi phòng ở mốc cũ là còn bị phiên này giữ.
        var candidateSchedule = schedule.Clone();
        candidateSchedule.Remove(sessionId);

        // Kiểm H1, H2, H5 ở mốc mới (CheckPlacement tự bỏ qua chính phiên đang kiểm).
        if (checker.CheckPlacement(candidateSchedule, sessionId, slotIndex, null).Count > 0)
        {
            return null;
        }

        // Qua được kiểm tra thì chia lại phòng cho cả nhóm ở mốc mới.
        var roomAssignment = GroupRoomAssigner.TryAssign(problem, candidateSchedule, sessionId, slotIndex);
        if (!roomAssignment.IsFeasible)
        {
            return null;
        }

        // Chia phòng được thì đặt phiên và áp dụng phòng cho cả nhóm.
        candidateSchedule.Place(sessionId, slotIndex);
        foreach (var sessionRoom in roomAssignment.RoomBySession)
        {
            candidateSchedule.SetRoom(sessionRoom.Key, sessionRoom.Value);
        }

        return candidateSchedule;
    }

    // ---------- HOÁN ĐỔI HAI PHIÊN ----------
    // Dùng ở: LocalSearch.Improve, SimulatedAnnealing.TryRandomSwap (đổi cả mốc lẫn phòng của hai phiên không chồng giờ).
    public static Schedule? TrySwap(SchedulingProblem problem, ConstraintChecker checker, Schedule schedule, int firstSessionId, int secondSessionId)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(checker);
        ArgumentNullException.ThrowIfNull(schedule);

        if (firstSessionId == secondSessionId
            || !schedule.TryGetPlacement(firstSessionId, out var firstPlacement)
            || !schedule.TryGetPlacement(secondSessionId, out var secondPlacement)
            || !firstPlacement.RoomId.HasValue || !secondPlacement.RoomId.HasValue)
        {
            return null;
        }

        var (firstDayIndex, firstInterval) = problem.GetSessionInterval(firstSessionId, firstPlacement.SlotIndex);
        var (secondDayIndex, secondInterval) = problem.GetSessionInterval(secondSessionId, secondPlacement.SlotIndex);
        if (firstDayIndex == secondDayIndex && firstInterval.Overlaps(secondInterval))
        {
            return null;
        }

        // Đặt cả hai trước khi kiểm: thời lượng riêng đi theo phiên, phòng đi theo vị trí.
        // Nhờ vậy H1/H2/H3 thấy cả phần giao nhau mới nếu hai phiên dài khác nhau.
        var candidateSchedule = schedule.Clone();
        candidateSchedule.Place(firstSessionId, secondPlacement.SlotIndex, secondPlacement.RoomId);
        candidateSchedule.Place(secondSessionId, firstPlacement.SlotIndex, firstPlacement.RoomId);
        if (checker.CheckPlacement(candidateSchedule, firstSessionId, secondPlacement.SlotIndex, secondPlacement.RoomId).Count > 0
            || checker.CheckPlacement(candidateSchedule, secondSessionId, firstPlacement.SlotIndex, firstPlacement.RoomId).Count > 0)
        {
            return null;
        }

        return candidateSchedule;
    }
}
