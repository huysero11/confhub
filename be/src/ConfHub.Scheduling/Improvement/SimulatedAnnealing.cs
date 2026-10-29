using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Improvement;

// Luyện kim có thể nhận bước xấu để thoát cực trị; luôn giữ riêng lịch tốt nhất đã gặp.
// Kết thúc bằng leo đồi trên lịch tốt nhất, dùng cùng bộ phép dời/hoán đổi với LocalSearch.
public sealed class SimulatedAnnealing
{
    private readonly SchedulingProblem _problem;
    private readonly ObjectiveWeights _weights;
    private readonly ImprovementOptions _options;
    private readonly ConstraintChecker _checker;
    private readonly ObjectiveCalculator _calculator;

    // Dùng ở: AnnealingScheduler.Run (tạo bộ luyện kim); T2.4 / T2.5 (cải thiện một lịch có sẵn).
    public SimulatedAnnealing(SchedulingProblem problem, ObjectiveWeights weights, ImprovementOptions options)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _problem = problem;
        _weights = weights;
        _options = options;
        _checker = new ConstraintChecker(problem);
        _calculator = new ObjectiveCalculator(problem, weights);
    }

    // ---------- ĐIỀU PHỐI LỊCH NHIỆT ----------
    // Dùng ở: AnnealingScheduler.Run (luyện kim rồi leo đồi trên lịch tốt nhất, giữ nguyên lịch đầu vào).
    public ImprovementResult Improve(Schedule start)
    {
        ArgumentNullException.ThrowIfNull(start);
        ValidateStart(start);

        var currentSchedule = start.Clone();
        var currentScore = _calculator.Evaluate(currentSchedule).Value;
        var bestSchedule = currentSchedule;
        var bestScore = currentScore;

        var random = new Random(_options.Seed);
        var temperature = _options.InitialTemperature;
        var iterations = 0;
        var acceptedMoves = 0;

        var sessionIds = BuildPlacedSessionIds(start);
        while (temperature >= _options.MinTemperature && sessionIds.Count > 0)
        {
            for (var step = 0; step < _options.StepsPerTemperature; step++)
            {
                iterations++;
                Schedule? candidateSchedule = null;

                if (random.Next(2) == 0)
                {
                    candidateSchedule = TryRandomMove(currentSchedule, sessionIds, random);
                }
                else
                {
                    candidateSchedule = TryRandomSwap(currentSchedule, sessionIds, random);
                }

                // Phép không hợp lệ vẫn tính một bước, không bốc lại đến khi thành công.
                if (candidateSchedule is null)
                {
                    continue;
                }

                // So lịch lân cận với lịch HIỆN TẠI (không phải lịch tốt nhất): Δ > 0 là bước xấu.
                var candidateScore = _calculator.Evaluate(candidateSchedule).Value;
                var scoreDifference = candidateScore - currentScore;
                if (scoreDifference < 0 || random.NextDouble() < Math.Exp(-scoreDifference / temperature))
                {
                    currentSchedule = candidateSchedule;
                    currentScore = candidateScore;
                    acceptedMoves++;

                    // Mỗi phép trả bản sao mới nên lịch tốt nhất không bị bước tiếp theo sửa mất.
                    if (currentScore < bestScore)
                    {
                        bestSchedule = currentSchedule;
                        bestScore = currentScore;
                    }
                }
            }

            temperature *= _options.CoolingRate;
        }

        var localResult = new LocalSearch(_problem, _weights, _options).Improve(bestSchedule);
        return new ImprovementResult(localResult.Schedule, localResult.Value, iterations + localResult.Iterations, acceptedMoves + localResult.AcceptedMoves);
    }

    // ---------- CHỌN PHÉP NGẪU NHIÊN ----------
    // Helper của Improve (cùng lớp): mọi phiên đã đặt đều có thể được bốc, kể cả phiên đóng góp C bằng 0.
    private static List<int> BuildPlacedSessionIds(Schedule schedule)
    {
        var sessionIds = new List<int>();
        foreach (var sessionId in schedule.PlacedSessionIds)
        {
            sessionIds.Add(sessionId);
        }

        return sessionIds;
    }

    // ---------- KIỂM LỊCH ĐẦU ----------
    // Helper của Improve (cùng lớp): chỉ nhận lịch hợp lệ đã gán phòng để phép hoán đổi có vị trí đầy đủ.
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

    // Helper của Improve (cùng lớp): bốc một phiên rồi bốc đều một mốc hợp lệ khác mốc hiện tại.
    private Schedule? TryRandomMove(Schedule schedule, List<int> sessionIds, Random random)
    {
        var randomSessionId = sessionIds[random.Next(sessionIds.Count)];
        schedule.TryGetPlacement(randomSessionId, out var placement);

        // Lấy các mốc hợp lệ khác mốc hiện tại của phiên.
        var targetSlots = new List<int>();
        foreach (var slotIndex in _problem.GetValidStartSlots(randomSessionId))
        {
            if (slotIndex != placement.SlotIndex)
            {
                targetSlots.Add(slotIndex);
            }
        }

        if (targetSlots.Count == 0)
        {
            return null;
        }

        var targetSlot = targetSlots[random.Next(targetSlots.Count)];
        return NeighborhoodMoves.TryMove(_problem, _checker, schedule, randomSessionId, targetSlot);
    }

    // Helper của Improve (cùng lớp): bốc phiên thứ nhất, rồi bốc đối tác trong các phiên không chồng giờ.
    private Schedule? TryRandomSwap(Schedule schedule, List<int> sessionIds, Random random)
    {
        var firstSessionId = sessionIds[random.Next(sessionIds.Count)];
        schedule.TryGetPlacement(firstSessionId, out var firstPlacement);
        var (firstDayIndex, firstInterval) = _problem.GetSessionInterval(firstSessionId, firstPlacement.SlotIndex);

        var partnerIds = new List<int>();
        foreach (var sessionId in sessionIds)
        {
            if (sessionId == firstSessionId)
            {
                continue;
            }

            schedule.TryGetPlacement(sessionId, out var secondPlacement);
            var (secondDayIndex, secondInterval) = _problem.GetSessionInterval(sessionId, secondPlacement.SlotIndex);
            if (firstDayIndex != secondDayIndex || !firstInterval.Overlaps(secondInterval))
            {
                partnerIds.Add(sessionId);
            }
        }

        if (partnerIds.Count == 0)
        {
            return null;
        }

        var secondSessionId = partnerIds[random.Next(partnerIds.Count)];
        return NeighborhoodMoves.TrySwap(_problem, _checker, schedule, firstSessionId, secondSessionId);
    }
}
