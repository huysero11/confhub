// Điểm vào console: đọc tham số, làm nóng, sinh dữ liệu một lần mỗi seed và xuất năm thực nghiệm.
using System.Diagnostics;
using System.Globalization;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;

namespace ConfHub.Experiments;

internal static class Program
{
    // ---------- ĐIỂM VÀO ----------
    // Chạy toàn bộ hoặc một thực nghiệm; tham số sai và lỗi ghi file trả mã thoát khác 0.
    public static int Main(string[] args)
    {
        try
        {
            var (seedCount, outputDirectory, only) = ParseArguments(args);
            var startedAt = DateTimeOffset.Now;
            var totalStopwatch = Stopwatch.StartNew();
            Directory.CreateDirectory(outputDirectory);
            Console.WriteLine("Làm nóng các nhánh thực nghiệm trên Small, seed 1...");
            var warmupTables = CreateTables(only);
            RunExperiments(warmupTables, "Small", 1, ProblemGenerator.Generate(GeneratorOptions.Small));

            var tables = CreateTables(only);
            (string Name, GeneratorOptions Options)[] scales =
            [
                ("Small", GeneratorOptions.Small),
                ("Medium", GeneratorOptions.Medium),
                ("Large", GeneratorOptions.Large),
            ];
            foreach (var scale in scales)
            {
                for (var seed = 1; seed <= seedCount; seed++)
                {
                    var data = ProblemGenerator.Generate(scale.Options with { Seed = seed });
                    RunExperiments(tables, scale.Name, seed, data);
                    Console.WriteLine(FormattableString.Invariant($"Xong {scale.Name}, seed {seed}/{seedCount}; tổng {totalStopwatch.Elapsed.TotalSeconds:F1} s."));
                }
            }

            foreach (var table in tables)
            {
                table.Write(outputDirectory);
            }

            SummaryWriter.Write(outputDirectory, tables, seedCount, startedAt, totalStopwatch.Elapsed);
            totalStopwatch.Stop();
            Console.WriteLine(FormattableString.Invariant($"Hoàn tất: {totalStopwatch.Elapsed.TotalSeconds:F1} s. Kết quả: {Path.GetFullPath(outputDirectory)}"));
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("Cách chạy: dotnet run -c Release --project tools/ConfHub.Experiments -- [--seeds 10] [--out ../docs/experiments/T2.4] [--only methods]");
            return 1;
        }
    }

    // ---------- THAM SỐ VÀ BẢNG ----------
    // Helper của Main (cùng lớp): chỉ chấp nhận tùy chọn đã đặc tả, số seed nguyên dương.
    private static (int SeedCount, string OutputDirectory, string? Only) ParseArguments(string[] args)
    {
        var seedCount = 10;
        var outputDirectory = "../docs/experiments/T2.4";
        string? only = null;
        var seenOptions = new HashSet<string>(StringComparer.Ordinal);
        for (var argumentIndex = 0; argumentIndex < args.Length; argumentIndex += 2)
        {
            var option = args[argumentIndex];
            if (!seenOptions.Add(option) || argumentIndex + 1 >= args.Length)
            {
                throw new ArgumentException($"Tùy chọn lặp hoặc thiếu giá trị: {option}.", nameof(args));
            }

            var argument = args[argumentIndex + 1];
            switch (option)
            {
                case "--seeds":
                    if (!int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out seedCount) || seedCount <= 0)
                    {
                        throw new ArgumentException("--seeds phải là số nguyên dương.", nameof(args));
                    }

                    break;
                case "--out":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        throw new ArgumentException("--out không được rỗng.", nameof(args));
                    }

                    outputDirectory = argument;
                    break;
                case "--only":
                    if (argument is not ("methods" or "weights" or "annealing" or "reschedule" or "rooms"))
                    {
                        throw new ArgumentException("--only nhận methods, weights, annealing, reschedule hoặc rooms.", nameof(args));
                    }

                    only = argument;
                    break;
                default:
                    throw new ArgumentException($"Tùy chọn không hỗ trợ: {option}.", nameof(args));
            }
        }

        return (seedCount, outputDirectory, only);
    }

    // Helper của Main (cùng lớp): tạo đúng bảng được yêu cầu, cùng cột chung cho mọi thực nghiệm.
    private static List<CsvTable> CreateTables(string? only)
    {
        List<CsvTable> tables =
        [
            new("methods"),
            new("weights", "WeightedObjective"),
            new("annealing"),
            new("reschedule", "DisruptedCount", "PenalizedObjective", "ChangedEntityId"),
            new("rooms", "FailedCount"),
        ];
        if (only is not null)
        {
            tables.RemoveAll(table => table.Name != only);
        }

        return tables;
    }

    // ---------- CHẠY TRÊN CÙNG DỮ LIỆU ----------
    // Helper của Main (cùng lớp): lịch M3 tham chiếu chỉ dựng khi cần xếp lại hoặc so phòng.
    private static void RunExperiments(List<CsvTable> tables, string size, int seed, GeneratedData data)
    {
        var needsBaseSchedule = tables.Exists(table => table.Name is "reschedule" or "rooms");
        var baseSchedule = needsBaseSchedule
            ? new LocalSearchScheduler(data.Problem, new ImprovementOptions { Seed = seed }).Run().Schedule
            : null;
        foreach (var table in tables)
        {
            switch (table.Name)
            {
                case "methods":
                    MethodExperiment.Run(table, size, seed, data);
                    break;
                case "weights":
                    WeightExperiment.Run(table, size, seed, data);
                    break;
                case "annealing":
                    AnnealingExperiment.Run(table, size, seed, data);
                    break;
                case "reschedule":
                    RescheduleExperiment.Run(table, size, seed, data, baseSchedule!);
                    break;
                case "rooms":
                    RoomExperiment.Run(table, size, seed, data, baseSchedule!);
                    break;
            }
        }
    }
}
