using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Improvement;

// Leo đồi chọn bước tốt nhất: mỗi vòng duyệt hết lân cận Q1–Q3 rồi mới nhận một bước.
// Chỉ cải thiện các phiên đã có trong lịch; không thử xếp thêm phiên còn thiếu.
public sealed class LocalSearch
{
    private const double ScoreTolerance = 1e-12;
    private readonly SchedulingProblem _problem;
    private readonly ConstraintChecker _checker;
    private readonly ObjectiveCalculator _calculator;
    private readonly ImprovementOptions _options;

    // Dùng ở: LocalSearchScheduler, RestartScheduler, SimulatedAnnealing (tạo bộ cải thiện theo cùng trọng số).
    public LocalSearch(SchedulingProblem problem, ObjectiveWeights weights, ImprovementOptions options)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _problem = problem;
        _options = options;
        _checker = new ConstraintChecker(problem);
        _calculator = new ObjectiveCalculator(problem, weights);
    }

    // ---------- ĐIỀU PHỐI TỪNG VÒNG ----------
    // Dùng ở: LocalSearchScheduler.Run, RestartScheduler.Run, SimulatedAnnealing.Improve (giảm f đến khi hết bước tốt hơn).
    public ImprovementResult Improve(Schedule start)
    {
        ArgumentNullException.ThrowIfNull(start);
        ValidateStart(start);

        var currentSchedule = start.Clone();
        var currentScore = _calculator.Evaluate(currentSchedule).Value;

        var iterations = 0;
        var acceptedMoves = 0;

        while (iterations < _options.MaxIterations)
        {
            iterations++;

            var bestScore = currentScore;
            Schedule? bestSchedule = null;

            // Lấy các phiên gây xung đột nhiều nhất để thử dời hoặc hoán đổi.
            var candidateIds = ConflictContribution.RankCandidates(_problem, currentSchedule, _options.CandidateCount);
            foreach (var candidateId in candidateIds)
            {
                // Thử dời sang từng mốc hợp lệ khác.
                foreach (var slotIndex in _problem.GetValidStartSlots(candidateId))
                {
                    var movedSchedule = NeighborhoodMoves.TryMove(_problem, _checker, currentSchedule, candidateId, slotIndex);
                    ConsiderCandidate(movedSchedule, ref bestSchedule, ref bestScore);
                }

                // Thử hoán đổi với từng phiên khác.
                foreach (var otherSession in _problem.Sessions)
                {
                    var swappedSchedule = NeighborhoodMoves.TrySwap(_problem, _checker, currentSchedule, candidateId, otherSession.Id);
                    ConsiderCandidate(swappedSchedule, ref bestSchedule, ref bestScore);
                }
            }

            if (bestSchedule is null)
            {
                break;
            }

            currentSchedule = bestSchedule;
            currentScore = bestScore;
            acceptedMoves++;
        }

        return new ImprovementResult(currentSchedule, currentScore, iterations, acceptedMoves);
    }

    // ---------- KIỂM VÀ CHẤM LỊCH ----------
    // Helper của Improve (cùng lớp): lịch đầu phải hợp lệ và mọi phiên đã đặt phải có phòng.
    private void ValidateStart(Schedule start)
    {
        if (_checker.Validate(start).Count > 0)
        {
            throw new ArgumentException("Lịch đầu có vi phạm ràng buộc cứng.", nameof(start));
        }

        foreach (var sessionId in start.PlacedSessionIds)
        {
            start.TryGetPlacement(sessionId, out var placement);
            if (!placement.RoomId.HasValue)
            {
                throw new ArgumentException("Phiên đã đặt trong lịch đầu phải có phòng.", nameof(start));
            }
        }
    }

    // Helper của Improve (cùng lớp): giữ lịch có f nhỏ nhất; hòa trong sai số thì giữ bước tìm thấy trước.
    private void ConsiderCandidate(Schedule? candidateSchedule, ref Schedule? bestSchedule, ref double bestScore)
    {
        if (candidateSchedule is null)
        {
            return;
        }

        var candidateScore = _calculator.Evaluate(candidateSchedule).Value;
        if (candidateScore < bestScore - ScoreTolerance)
        {
            bestScore = candidateScore;
            bestSchedule = candidateSchedule;
        }
    }
}
