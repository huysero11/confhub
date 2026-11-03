// Dò lưới nhiệt độ đầu và số bước mỗi mức nhiệt của M4, không sửa mặc định thuật toán.
using System.Diagnostics;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;

namespace ConfHub.Experiments;

internal static class AnnealingExperiment
{
    // Chạy đủ tích Descartes ba nhiệt độ và hai số bước.
    public static void Run(CsvTable table, string size, int seed, GeneratedData data)
    {
        double[] temperatures = [0.05, 0.01, 0.002];
        int[] stepCounts = [20, 100];
        var calculator = new MetricsCalculator(data.Problem, data.Interests, ObjectiveWeights.Default);
        foreach (var temperature in temperatures)
        {
            foreach (var stepCount in stepCounts)
            {
                var options = new ImprovementOptions
                {
                    Seed = seed,
                    InitialTemperature = temperature,
                    StepsPerTemperature = stepCount,
                };
                var variant = FormattableString.Invariant($"T={temperature};steps={stepCount}");
                var stopwatch = Stopwatch.StartNew();
                var schedule = new AnnealingScheduler(data.Problem, options).Run().Schedule;
                stopwatch.Stop();
                table.AddMetrics(size, seed, variant, calculator.Calculate(schedule), stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
