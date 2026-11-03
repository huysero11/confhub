// So xếp lại có phạt xáo trộn với chạy M3 từ đầu sau mất phòng hoặc diễn giả bận ngày 0.
using System.Diagnostics;
using ConfHub.Scheduling.Evaluation;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Rescheduling;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Experiments;

internal static class RescheduleExperiment
{
    // ---------- ĐIỀU PHỐI KỊCH BẢN ----------
    // Chọn đối tượng biến động theo lịch gốc, thử đủ bốn trọng số trên cùng bài toán mới.
    public static void Run(CsvTable table, string size, int seed, GeneratedData data, Schedule baseSchedule)
    {
        var smallestRoom = data.Problem.Rooms[0];
        foreach (var room in data.Problem.Rooms)
        {
            if (room.Capacity < smallestRoom.Capacity)
            {
                smallestRoom = room;
            }
        }

        var withoutRoom = ChangeScenario.RemoveRoom(data, smallestRoom.Id);
        RunScenario(table, size, seed, data, baseSchedule, "RemoveRoom", smallestRoom.Id, withoutRoom);

        var speakerId = FindBusiestSpeaker(data.Problem, baseSchedule);
        SchedulingProblem? withoutSpeaker = null;
        if (speakerId.HasValue)
        {
            withoutSpeaker = ChangeScenario.MakeSpeakerUnavailable(data, speakerId.Value, 0);
        }

        RunScenario(table, size, seed, data, baseSchedule, "SpeakerDay0", speakerId, withoutSpeaker);
    }

    // Helper của Run (cùng lớp): chọn diễn giả còn ngày rảnh có nhiều phiên ngày 0 nhất, hòa lấy mã nhỏ.
    private static int? FindBusiestSpeaker(SchedulingProblem problem, Schedule baseSchedule)
    {
        var sessionCountBySpeaker = new Dictionary<int, int>();
        foreach (var session in problem.Sessions)
        {
            if (!baseSchedule.TryGetPlacement(session.Id, out var placement)
                || problem.Timeline.Slots[placement.SlotIndex].DayIndex != 0)
            {
                continue;
            }

            foreach (var speakerId in session.SpeakerIds.Distinct())
            {
                sessionCountBySpeaker.TryGetValue(speakerId, out var sessionCount);
                sessionCountBySpeaker[speakerId] = sessionCount + 1;
            }
        }

        int? selectedSpeakerId = null;
        var largestSessionCount = 0;
        foreach (var speakerCount in sessionCountBySpeaker)
        {
            if (!HasAvailabilityOutsideDayZero(problem, speakerCount.Key))
            {
                continue;
            }

            if (speakerCount.Value > largestSessionCount
                || (speakerCount.Value == largestSessionCount && (!selectedSpeakerId.HasValue || speakerCount.Key < selectedSpeakerId.Value)))
            {
                selectedSpeakerId = speakerCount.Key;
                largestSessionCount = speakerCount.Value;
            }
        }

        return selectedSpeakerId;
    }

    // Helper của FindBusiestSpeaker (cùng lớp): không khai giờ rảnh nghĩa là rảnh mọi ngày.
    private static bool HasAvailabilityOutsideDayZero(SchedulingProblem problem, int speakerId)
    {
        var hasDeclaredAvailability = false;
        foreach (var availability in problem.SpeakerAvailabilities)
        {
            if (availability.SpeakerId != speakerId)
            {
                continue;
            }

            hasDeclaredAvailability = true;
            if (availability.DayIndex != 0)
            {
                return true;
            }
        }

        return !hasDeclaredAvailability;
    }

    // ---------- ĐO XẾP LẠI ----------
    // Helper của Run (cùng lớp): mỗi biến gồm một lần xếp lại và một lần M3 mới, cùng cách chấm D và f'.
    private static void RunScenario(
        CsvTable table,
        string size,
        int seed,
        GeneratedData data,
        Schedule baseSchedule,
        string scenario,
        int? changedEntityId,
        SchedulingProblem? changedProblem)
    {
        double[] disruptionWeights = [0.1, 0.5, 1, 2];
        string[] methods = ["Reschedule", "M3Fresh"];
        foreach (var disruptionWeight in disruptionWeights)
        {
            foreach (var method in methods)
            {
                var variant = FormattableString.Invariant($"{scenario};w4={disruptionWeight};{method}");
                if (changedProblem is null)
                {
                    table.AddSkipped(size, seed, variant);
                    continue;
                }

                var options = new ImprovementOptions { Seed = seed };
                var stopwatch = Stopwatch.StartNew();
                Schedule schedule;
                if (method == "Reschedule")
                {
                    var rescheduleOptions = new RescheduleOptions { DisruptionWeight = disruptionWeight, Improvement = options };
                    schedule = new Rescheduler(changedProblem, rescheduleOptions).Run(baseSchedule).Schedule;
                }
                else
                {
                    schedule = new LocalSearchScheduler(changedProblem, options).Run().Schedule;
                }

                stopwatch.Stop();
                var disruption = new DisruptionPenalty(changedProblem, baseSchedule, disruptionWeight);
                var penalizedObjective = new ObjectiveCalculator(changedProblem, ObjectiveWeights.Default, disruption).Evaluate(schedule);
                var metrics = new MetricsCalculator(changedProblem, data.Interests, ObjectiveWeights.Default).Calculate(schedule);
                table.AddMetrics(
                    size,
                    seed,
                    variant,
                    metrics,
                    stopwatch.Elapsed.TotalMilliseconds,
                    penalizedObjective.DisruptedCount,
                    penalizedObjective.Value,
                    changedEntityId!.Value);
            }
        }
    }
}
