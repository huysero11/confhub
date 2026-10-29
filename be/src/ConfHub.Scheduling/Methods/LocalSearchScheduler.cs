using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Methods;

// M3: tạo lịch tham lam rồi cải thiện cục bộ trên các phiên đã xếp.
// Giữ nguyên danh sách chưa xếp và lý do từ lần tham lam tương ứng.
public sealed class LocalSearchScheduler
{
    private readonly GreedyScheduler _greedyScheduler;
    private readonly LocalSearch _localSearch;

    // Dùng ở: T2.4 và test (tạo phương pháp M3, có thể thay trọng số khi phân tích độ nhạy).
    public LocalSearchScheduler(SchedulingProblem problem, ImprovementOptions? options = null, ObjectiveWeights? weights = null)
    {
        var selectedWeights = weights ?? ObjectiveWeights.Default;
        _greedyScheduler = new GreedyScheduler(problem, selectedWeights);
        _localSearch = new LocalSearch(problem, selectedWeights, options ?? new ImprovementOptions());
    }

    // Dùng ở: T2.4 và test (chạy trọn luồng tham lam → cục bộ).
    public SchedulingResult Run()
    {
        var greedyResult = _greedyScheduler.Run();
        var improvementResult = _localSearch.Improve(greedyResult.Schedule);
        return new SchedulingResult(improvementResult.Schedule, greedyResult.UnplacedSessions);
    }
}
