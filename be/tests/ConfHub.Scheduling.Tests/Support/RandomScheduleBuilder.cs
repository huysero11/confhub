using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Tests.Support;

// Dựng lịch ngẫu nhiên nhưng luôn hợp lệ: thử mốc / phòng ngẫu nhiên cho từng phiên,
// chỉ nhận khi CheckPlacement không báo vi phạm nào.
public static class RandomScheduleBuilder
{
    private const int TriesPerSession = 200;

    public static Schedule Build(SchedulingProblem problem, int seed)
    {
        var random = new Random(seed);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();

        foreach (var session in problem.Sessions)
        {
            for (var attempt = 0; attempt < TriesPerSession; attempt++)
            {
                var slotIndex = random.Next(problem.Timeline.Slots.Count);
                var room = problem.Rooms[random.Next(problem.Rooms.Count)];

                // Một phần phiên để chưa gán phòng, để cả hai trường hợp đều được thử.
                int? roomId = room.Id;
                if (random.Next(5) == 0)
                {
                    roomId = null;
                }

                var violations = checker.CheckPlacement(schedule, session.Id, slotIndex, roomId);
                if (violations.Count == 0)
                {
                    schedule.Place(session.Id, slotIndex, roomId);
                    break;
                }
            }
        }

        return schedule;
    }
}
