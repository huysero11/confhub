// So sánh sáu phương pháp trên cùng một bài toán và cùng trọng số chấm mặc định.
using System.Diagnostics;
using ConfHub.Scheduling.Baselines;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;

namespace ConfHub.Experiments;

internal static class MethodExperiment
{
    // Chạy mỗi phương pháp đúng một lần, đo cả khởi tạo và xếp lịch, không đo tính chỉ số.
    public static void Run(CsvTable table, string size, int seed, GeneratedData data)
    {
        var problem = data.Problem;
        var options = new ImprovementOptions { Seed = seed };
        var calculator = new MetricsCalculator(problem, data.Interests, ObjectiveWeights.Default);
        string[] methods = ["M1", "M2", "Greedy", "M3", "M3R", "M4"];
        foreach (var method in methods)
        {
            var stopwatch = Stopwatch.StartNew();
            var schedulingResult = method switch
            {
                "M1" => new FirstFitScheduler(problem).Run(),
                "M2" => new InterestBlindScheduler(problem).Run(),
                "Greedy" => new GreedyScheduler(problem, ObjectiveWeights.Default).Run(),
                "M3" => new LocalSearchScheduler(problem, options).Run(),
                "M3R" => new RestartScheduler(problem, options).Run(),
                "M4" => new AnnealingScheduler(problem, options).Run(),
                _ => throw new InvalidOperationException("Phương pháp không được hỗ trợ."),
            };
            stopwatch.Stop();
            table.AddMetrics(size, seed, method, calculator.Calculate(schedulingResult.Schedule), stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
