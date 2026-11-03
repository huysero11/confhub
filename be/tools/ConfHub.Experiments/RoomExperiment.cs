// So phòng của lịch M3 (Hungarian theo nhóm) với gán tham lam trên đúng các mốc đó.
using System.Diagnostics;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Experiments;

internal static class RoomExperiment
{
    // Hungarian là lịch tham chiếu đã có; chỉ đo thời gian lần gán lại tham lam độc lập.
    public static void Run(CsvTable table, string size, int seed, GeneratedData data, Schedule baseSchedule)
    {
        var calculator = new MetricsCalculator(data.Problem, data.Interests, ObjectiveWeights.Default);
        table.AddMetrics(size, seed, "Hungarian", calculator.Calculate(baseSchedule), null, 0);
        var stopwatch = Stopwatch.StartNew();
        var assignment = GreedyRoomBaseline.Assign(data.Problem, baseSchedule);
        stopwatch.Stop();
        table.AddMetrics(size, seed, "GreedyRoom", calculator.Calculate(assignment.Schedule), stopwatch.Elapsed.TotalMilliseconds, assignment.FailedSessionIds.Count);
    }
}
