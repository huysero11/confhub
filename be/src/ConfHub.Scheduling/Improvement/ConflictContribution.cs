using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Improvement;

// Q1: đếm xung đột mà từng phiên đang gây ra rồi lấy các phiên đóng góp cao nhất.
// Đóng góp chỉ đo C; đây là cách thu hẹp tìm kiếm, không phải toàn bộ hàm mục tiêu f.
public static class ConflictContribution
{
    // ---------- CHỌN ỨNG VIÊN ----------
    // Dùng ở: LocalSearch.Improve (chỉ thử tối đa K phiên có đóng góp dương mỗi vòng).
    public static IReadOnlyList<int> RankCandidates(SchedulingProblem problem, Schedule schedule, int candidateCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateCount);

        var contributions = Calculate(problem, schedule);

        var candidateIds = new List<int>();
        var initialPositions = new Dictionary<int, int>();
        foreach (var session in problem.Sessions)
        {
            if (contributions.TryGetValue(session.Id, out var contribution) && contribution > 0)
            {
                initialPositions[session.Id] = candidateIds.Count;
                candidateIds.Add(session.Id);
            }
        }

        candidateIds.Sort((firstId, secondId) =>
        {
            var contributionComparison = contributions[secondId].CompareTo(contributions[firstId]);
            if (contributionComparison != 0)
            {
                return contributionComparison;
            }

            return initialPositions[firstId].CompareTo(initialPositions[secondId]);
        });

        if (candidateIds.Count > candidateCount)
        {
            candidateIds.RemoveRange(candidateCount, candidateIds.Count - candidateCount);
        }

        return candidateIds;
    }

    // ---------- TÍNH ĐÓNG GÓP ----------
    // Kết quả dạng map: contributions[A] = 35 nghĩa là A đang trùng giờ với 35 lượt người muốn dự.
    // Dùng ở: RankCandidates (cùng lớp, tính điểm Q1); test và T2.4 (giải thích phiên gây xung đột).
    public static IReadOnlyDictionary<int, int> Calculate(SchedulingProblem problem, Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(schedule);

        // Lấy danh sách phiên đã đặt kèm ngày và khoảng giờ, điểm ban đầu bằng 0.
        var contributions = new Dictionary<int, int>();
        var placedSessions = new List<PlacedSession>();
        foreach (var placedSessionId in schedule.PlacedSessionIds)
        {
            schedule.TryGetPlacement(placedSessionId, out var placement);
            var (dayIndex, interval) = problem.GetSessionInterval(placedSessionId, placement.SlotIndex);
            placedSessions.Add(new PlacedSession(placedSessionId, dayIndex, interval, placement.RoomId));
            contributions[placedSessionId] = 0;
        }

        // Mỗi cặp trùng giờ duyệt một lần, cộng W[i][j] vào điểm của cả hai phiên.
        for (var i = 0; i < placedSessions.Count; i++)
        {
            for (var j = i + 1; j < placedSessions.Count; j++)
            {
                var firstSession = placedSessions[i];
                var secondSession = placedSessions[j];
                if (firstSession.DayIndex == secondSession.DayIndex
                    && firstSession.Interval.Overlaps(secondSession.Interval))
                {
                    var conflictCount = problem.InterestMatrix.Get(firstSession.SessionId, secondSession.SessionId);
                    contributions[firstSession.SessionId] += conflictCount;
                    contributions[secondSession.SessionId] += conflictCount;
                }
            }
        }

        return contributions;
    }
}
