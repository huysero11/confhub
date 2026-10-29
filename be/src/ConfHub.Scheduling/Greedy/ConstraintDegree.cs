using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Greedy;

// Độ ràng buộc nhỏ nghĩa là ít lựa chọn, cần xếp trước.
// GreedyScheduler dùng thứ tự này khi tầng gọi không truyền thứ tự riêng.
public static class ConstraintDegree
{
    /*
    - Trả ra một mảng sessionId sắp xếp tăng dần theo độ ràng buộc
        - Tính mảng degrees[]
        - Sort theo mảng degrees đấy. Tuy nhiên trong c# những cái bằng nhau không tự theo thứ tự, nên phải có mảng
            vị trí ban đầu
    */
    // Dùng ở: GreedyScheduler.Run (thứ tự mặc định khi không truyền sessionOrder).
    // Dùng ở: RestartScheduler.Run (lấy thứ tự gốc trước khi xáo trộn các lần sau).
    public static IReadOnlyList<int> OrderSessions(SchedulingProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        var sessionIds = new List<int>();
        var degrees = new Dictionary<int, int>();
        var initialPositions = new Dictionary<int, int>();
        foreach (var session in problem.Sessions)
        {
            initialPositions[session.Id] = sessionIds.Count;
            degrees[session.Id] = CalculateDegree(problem, session.Id);
            sessionIds.Add(session.Id);
        }

        sessionIds.Sort((firstId, secondId) =>
        {
            var degreeComparison = degrees[firstId].CompareTo(degrees[secondId]);
            if (degreeComparison != 0)
            {
                return degreeComparison;
            }

            return initialPositions[firstId].CompareTo(initialPositions[secondId]);
        });

        return sessionIds;
    }

    // Dùng ở: OrderSessions (cùng lớp); test kiểm công thức.
    // Độ ràng buộc = số mốc bắt đầu hợp lệ của phiên − số phiên của diễn giả bận nhất
    public static int CalculateDegree(SchedulingProblem problem, int sessionId)
    {
        ArgumentNullException.ThrowIfNull(problem);

        var session = problem.GetSession(sessionId);
        var busiestSpeakerSessionCount = 0;
        foreach (var speakerId in session.SpeakerIds)
        {
            var speakerSessionCount = 0;
            foreach (var otherSession in problem.Sessions)
            {
                if (otherSession.SpeakerIds.Contains(speakerId))
                {
                    speakerSessionCount++;
                }
            }

            busiestSpeakerSessionCount = Math.Max(busiestSpeakerSessionCount, speakerSessionCount);
        }

        return problem.GetValidStartSlots(sessionId).Count - busiestSpeakerSessionCount;
    }
}
