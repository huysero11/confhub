// Đo độ nhạy ba trọng số của M3; mỗi lịch có cả f mặc định và f theo trọng số đang thử.
using System.Diagnostics;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;

namespace ConfHub.Experiments;

internal static class WeightExperiment
{
    // Chạy năm bộ trọng số cố định của T2.4, giữ nguyên seed và dữ liệu.
    public static void Run(CsvTable table, string size, int seed, GeneratedData data)
    {
        ObjectiveWeights[] weightSets =
        [
            ObjectiveWeights.Default,
            new(1, 0, 0),
            new(0.5, 0.3, 0.2),
            new(0.34, 0.33, 0.33),
            new(0.9, 0.05, 0.05),
        ];
        var calculator = new MetricsCalculator(data.Problem, data.Interests, ObjectiveWeights.Default);
        foreach (var weights in weightSets)
        {
            var variant = FormattableString.Invariant($"w=({weights.Conflict};{weights.Load};{weights.Waste})");
            var stopwatch = Stopwatch.StartNew();
            var schedule = new LocalSearchScheduler(data.Problem, new ImprovementOptions { Seed = seed }, weights).Run().Schedule;
            stopwatch.Stop();
            var weightedObjective = new ObjectiveCalculator(data.Problem, weights).Evaluate(schedule).Value;
            table.AddMetrics(size, seed, variant, calculator.Calculate(schedule), stopwatch.Elapsed.TotalMilliseconds, weightedObjective);
        }
    }
}
