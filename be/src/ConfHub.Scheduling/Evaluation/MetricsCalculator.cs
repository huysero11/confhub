// Tính chỉ số của lịch và mức đáp ứng nguyện vọng theo từng người tham dự.
using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Evaluation;

public sealed class MetricsCalculator
{
    private readonly SchedulingProblem _problem;
    private readonly ObjectiveCalculator _objectiveCalculator;
    private readonly ConstraintChecker _constraintChecker;
    private readonly Dictionary<int, List<int>> _sessionIdsByAttendee = [];

    // Gom nguyện vọng không trùng và giữ bộ chấm điểm dùng chung cho các lịch.
    public MetricsCalculator(SchedulingProblem problem, IReadOnlyList<InterestEntry> interests, ObjectiveWeights weights)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(interests);
        ArgumentNullException.ThrowIfNull(weights);
        _problem = problem;
        _objectiveCalculator = new ObjectiveCalculator(problem, weights);
        _constraintChecker = new ConstraintChecker(problem);

        foreach (var interest in interests)
        {
            problem.GetSession(interest.SessionId);
            if (!_sessionIdsByAttendee.TryGetValue(interest.AttendeeId, out var sessionIds))
            {
                sessionIds = [];
                _sessionIdsByAttendee.Add(interest.AttendeeId, sessionIds);
            }

            if (!sessionIds.Contains(interest.SessionId))
            {
                sessionIds.Add(interest.SessionId);
            }
        }
    }

    // ---------- TỔNG HỢP CHỈ SỐ ----------
    // Chấm lịch theo trọng số đã nhận, không thay đổi lịch hoặc dữ liệu bài toán.
    public ScheduleMetrics Calculate(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var objective = _objectiveCalculator.Evaluate(schedule);
        var placedCount = schedule.PlacedSessionIds.Count;
        return new ScheduleMetrics(
            _problem.Sessions.Count,
            placedCount,
            placedCount / (double)_problem.Sessions.Count,
            objective.ConflictCount,
            objective.LoadStdDev,
            objective.WastedSeats,
            objective.Value,
            CalculateNoLossAttendeeRate(schedule),
            CalculateRoomFillRate(schedule),
            _constraintChecker.Validate(schedule).Count);
    }

    // ---------- NGUYỆN VỌNG VÀ SỨC CHỨA ----------
    // Helper của Calculate (cùng lớp): mẫu số chỉ gồm người có ít nhất một nguyện vọng.
    private double CalculateNoLossAttendeeRate(Schedule schedule)
    {
        if (_sessionIdsByAttendee.Count == 0)
        {
            return 0;
        }

        var noLossCount = 0;
        foreach (var sessionIds in _sessionIdsByAttendee.Values)
        {
            if (!HasLostSession(schedule, sessionIds))
            {
                noLossCount++;
            }
        }

        return noLossCount / (double)_sessionIdsByAttendee.Count;
    }

    // Helper của CalculateNoLossAttendeeRate (cùng lớp): thiếu phiên hoặc chồng khoảng cùng ngày là mất.
    private bool HasLostSession(Schedule schedule, List<int> sessionIds)
    {
        for (var firstIndex = 0; firstIndex < sessionIds.Count; firstIndex++)
        {
            var firstSessionId = sessionIds[firstIndex];
            if (!schedule.TryGetPlacement(firstSessionId, out var firstPlacement))
            {
                return true;
            }

            var (firstDay, firstInterval) = _problem.GetSessionInterval(firstSessionId, firstPlacement.SlotIndex);
            for (var secondIndex = firstIndex + 1; secondIndex < sessionIds.Count; secondIndex++)
            {
                var secondSessionId = sessionIds[secondIndex];
                if (!schedule.TryGetPlacement(secondSessionId, out var secondPlacement))
                {
                    return true;
                }

                var (secondDay, secondInterval) = _problem.GetSessionInterval(secondSessionId, secondPlacement.SlotIndex);
                if (firstDay == secondDay && firstInterval.Overlaps(secondInterval))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Helper của Calculate (cùng lớp): chỉ cộng phiên đã có phòng; chưa có sức chứa thì trả 0.
    private double CalculateRoomFillRate(Schedule schedule)
    {
        long totalSize = 0;
        long totalCapacity = 0;
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            schedule.TryGetPlacement(sessionId, out var placement);
            if (placement.RoomId.HasValue)
            {
                totalSize += _problem.GetSessionSize(sessionId);
                totalCapacity += _problem.GetRoom(placement.RoomId.Value).Capacity;
            }
        }

        return totalCapacity == 0 ? 0 : totalSize / (double)totalCapacity;
    }
}
