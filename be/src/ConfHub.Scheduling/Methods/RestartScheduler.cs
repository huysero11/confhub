using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Methods;

// M3R: chạy M3 nhiều lần với thứ tự phiên khác nhau; lần đầu luôn là M3 thông thường.
// Ưu tiên số phiên xếp được trước f để lịch thiếu phiên không thắng nhờ ít xung đột.
public sealed class RestartScheduler
{
    private const double ScoreTolerance = 1e-12;
    private readonly SchedulingProblem _problem;
    private readonly ImprovementOptions _options;
    private readonly GreedyScheduler _greedyScheduler;
    private readonly LocalSearch _localSearch;

    // Dùng ở: T2.4 và test (tạo phương pháp M3R với số lần thử và Seed cố định).
    public RestartScheduler(SchedulingProblem problem, ImprovementOptions? options = null, ObjectiveWeights? weights = null)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _problem = problem;
        _options = options ?? new ImprovementOptions();
        _options.Validate();

        var selectedWeights = weights ?? ObjectiveWeights.Default;
        _greedyScheduler = new GreedyScheduler(problem, selectedWeights);
        _localSearch = new LocalSearch(problem, selectedWeights, _options);
    }

    // ---------- CHẠY VÀ GIỮ LẦN TỐT NHẤT ----------
    // Dùng ở: T2.4 và test (chạy nhiều thứ tự, trả lịch và danh sách chưa xếp của cùng một lần thắng).
    public SchedulingResult Run()
    {
        var initialOrder = ConstraintDegree.OrderSessions(_problem);
        var firstGreedyResult = _greedyScheduler.Run(initialOrder);
        var firstImprovement = _localSearch.Improve(firstGreedyResult.Schedule);

        var bestResult = new SchedulingResult(firstImprovement.Schedule, firstGreedyResult.UnplacedSessions);
        var bestPlacedCount = bestResult.Schedule.PlacedSessionIds.Count;
        var bestScore = firstImprovement.Value;

        // Lần 1 ở trên là lần khởi động thứ nhất, nên vòng này chạy thêm RestartCount - 1 lần.
        var random = new Random(_options.Seed);
        for (var restart = 1; restart < _options.RestartCount; restart++)
        {
            var sessionOrder = new List<int>(initialOrder);
            Shuffle(sessionOrder, random);

            var greedyResult = _greedyScheduler.Run(sessionOrder);
            var improvementResult = _localSearch.Improve(greedyResult.Schedule);
            var improvementPlacedCount = improvementResult.Schedule.PlacedSessionIds.Count;

            if (improvementPlacedCount > bestPlacedCount
                || (improvementPlacedCount == bestPlacedCount && improvementResult.Value < bestScore - ScoreTolerance))
            {
                bestResult = new SchedulingResult(improvementResult.Schedule, greedyResult.UnplacedSessions);
                bestPlacedCount = improvementPlacedCount;
                bestScore = improvementResult.Value;
            }
        }

        return bestResult;
    }

    // ---------- XÁO TRỘN THỨ TỰ ----------
    // Helper của Run (cùng lớp): Fisher–Yates, mỗi bước bốc đều một phần tử trong đoạn [0, i] đưa về vị trí i.
    private static void Shuffle(List<int> sessionOrder, Random random)
    {
        // Dừng ở i = 1: đoạn còn 1 phần tử thì không còn gì để đổi.
        for (var i = sessionOrder.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);

            // swap(sessionOrder[i], sessionOrder[j])
            (sessionOrder[j], sessionOrder[i]) = (sessionOrder[i], sessionOrder[j]);
        }
    }
}
