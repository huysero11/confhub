using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Baselines;

// M2 dùng tham lam nhưng bỏ trọng số xung đột nguyện vọng, giữ tỉ lệ tải : lãng phí là 2 : 1.
// Quy mô phiên vẫn dùng cho sức chứa; tầng gọi chấm kết quả bằng trọng số chung.
public sealed class InterestBlindScheduler
{
    private readonly SchedulingProblem _problem;

    // Dùng ở: T2.4 và test (tạo phương pháp đối chứng M2).
    public InterestBlindScheduler(SchedulingProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _problem = problem;
    }

    public static ObjectiveWeights Weights { get; } = new(0, 2.0 / 3.0, 1.0 / 3.0);

    // Dùng ở: T2.4 và test (chạy M2 qua cùng thuật toán tham lam với trọng số không có C).
    public SchedulingResult Run()
    {
        return new GreedyScheduler(_problem, Weights).Run();
    }
}
