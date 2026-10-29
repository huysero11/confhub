using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Methods;

// M4: tham lam → luyện kim → cục bộ cuối; SimulatedAnnealing đã chứa bước cục bộ.
// Danh sách chưa xếp vẫn thuộc lần tham lam ban đầu.
public sealed class AnnealingScheduler
{
    private readonly GreedyScheduler _greedyScheduler;
    private readonly SimulatedAnnealing _annealing;

    // Dùng ở: T2.4 và test (tạo phương pháp M4 với lịch nhiệt và Seed đã chọn).
    public AnnealingScheduler(SchedulingProblem problem, ImprovementOptions? options = null, ObjectiveWeights? weights = null)
    {
        var selectedWeights = weights ?? ObjectiveWeights.Default;
        _greedyScheduler = new GreedyScheduler(problem, selectedWeights);
        _annealing = new SimulatedAnnealing(problem, selectedWeights, options ?? new ImprovementOptions());
    }

    // Dùng ở: T2.4 và test (chạy trọn luồng M4, trả lịch tốt nhất sau cục bộ).
    public SchedulingResult Run()
    {
        var greedyResult = _greedyScheduler.Run();
        var improvementResult = _annealing.Improve(greedyResult.Schedule);
        return new SchedulingResult(improvementResult.Schedule, greedyResult.UnplacedSessions);
    }
}
