// Tổng hợp CSV trong bộ nhớ thành trung bình ± độ lệch chuẩn mẫu qua các seed.
using System.Globalization;
using System.Text;

namespace ConfHub.Experiments;

internal static class SummaryWriter
{
    // ---------- XUẤT TÓM TẮT ----------
    // Ghi bảng chia theo thực nghiệm / quy mô / biến; ca skipped không đi vào thống kê.
    public static void Write(string outputDirectory, List<CsvTable> tables, int seedCount, DateTimeOffset startedAt, TimeSpan elapsed)
    {
        using var writer = new StreamWriter(Path.Combine(outputDirectory, "summary.md"), false, new UTF8Encoding(false));
        writer.NewLine = "\r\n";
        writer.WriteLine("# T2.4 — Kết quả thực nghiệm");
        writer.WriteLine();
        writer.WriteLine($"- Máy: `{Environment.MachineName}`; .NET: `{Environment.Version}`; bản Release.");
        writer.WriteLine(FormattableString.Invariant($"- Bắt đầu: {startedAt:yyyy-MM-dd HH:mm:ss zzz}; seed: 1–{seedCount}."));
        writer.WriteLine(FormattableString.Invariant($"- Thời gian tổng đến lúc viết tóm tắt: {elapsed.TotalSeconds:F1} s (gồm làm nóng, sinh dữ liệu, đo và ghi CSV)."));
        writer.WriteLine("- Mỗi ô: trung bình ± độ lệch chuẩn mẫu (chia n−1); n=1 quy ước độ lệch chuẩn 0.");
        writer.WriteLine("- Mỗi phép đo chạy một lần sau lượt làm nóng Small/seed 1; ms chỉ gồm khởi tạo thuật toán và xếp lịch.");
        writer.WriteLine("- C: xung đột; B: độ lệch tải; S: ghế lãng phí; f: trọng số mặc định; H: vi phạm cứng.");
        writer.WriteLine("- Xếp: số phiên đã đặt; tỷ lệ xếp / không mất / lấp đầy ở thang 0–1, không phải phần trăm.");
        writer.WriteLine("- Không có người có nguyện vọng hoặc không có sức chứa được gán: tỷ lệ tương ứng quy ước 0.");
        writer.WriteLine("- Phòng chưa gán không vào tỷ lệ lấp đầy; phiên gán phòng tham lam thất bại bị loại khỏi lịch kết quả.");
        writer.WriteLine("- Hungarian trong rooms là phòng sẵn có của lịch M3, không phải tối ưu toàn bộ lịch; ms là n/a vì không đo lại.");
        writer.WriteLine("- FailedCount chỉ đếm thất bại khi gán lại các phiên đã có mốc; không gồm phiên M3 chưa xếp được.");
        writer.WriteLine("- Giảm C: tính (C_mốc − C_phương_pháp)/C_mốc × 100 từng seed rồi tổng hợp; mốc có C=0 thì n/a.");
        writer.WriteLine("- skipped: không có diễn giả còn giờ rảnh ở ngày khác và có phiên ngày 0, bỏ khỏi thống kê; n/a: không có phép đo.");
        writer.WriteLine("- Chỉ so sánh tương đối trên dữ liệu mô phỏng, không kiểm định thống kê; cần đọc C cùng tỷ lệ xếp.");
        writer.WriteLine("- Chỉ số 9 và 11 cần dữ liệu thật, chưa đo ở T2.4. Không thay mặc định thuật toán.");

        foreach (var table in tables)
        {
            WriteExperiment(writer, table);
        }
    }

    // Helper của Write (cùng lớp): gán nhãn ngắn cho biến để bảng không bị quá rộng.
    private static void WriteExperiment(StreamWriter writer, CsvTable table)
    {
        var variants = new List<string>();
        var sizes = new List<string>();
        foreach (var row in table.Rows)
        {
            if (!variants.Contains(row[2]))
            {
                variants.Add(row[2]);
            }

            if (!sizes.Contains(row[0]))
            {
                sizes.Add(row[0]);
            }
        }

        writer.WriteLine();
        writer.WriteLine($"## {table.Name}");
        writer.WriteLine();
        for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            writer.WriteLine(FormattableString.Invariant($"- V{variantIndex + 1}: `{variants[variantIndex]}`"));
        }

        foreach (var size in sizes)
        {
            writer.WriteLine();
            writer.WriteLine($"### {size}");
            writer.WriteLine();
            WriteMetricTable(
                writer,
                table,
                size,
                variants,
                ["ConflictCount", "LoadStdDev", "WastedSeats", "Objective", "ElapsedMs"],
                ["C", "B", "S", "f", "ms"]);
            WriteMetricTable(
                writer,
                table,
                size,
                variants,
                ["PlacedCount", "PlacementRate", "NoLossAttendeeRate", "RoomFillRate", "ViolationCount"],
                ["Xếp", "Tỷ lệ xếp", "Không mất", "Lấp đầy", "H"]);

            var extraColumns = new List<string>();
            for (var columnIndex = 15; columnIndex < table.Headers.Length; columnIndex++)
            {
                if (table.Headers[columnIndex] != "ChangedEntityId")
                {
                    extraColumns.Add(table.Headers[columnIndex]);
                }
            }

            if (extraColumns.Count > 0)
            {
                WriteMetricTable(writer, table, size, variants, extraColumns.ToArray(), extraColumns.ToArray());
            }

            if (table.Name == "methods")
            {
                WriteReductions(writer, table, size, variants);
            }
        }
    }

    // ---------- CÁC BẢNG CHỈ SỐ ----------
    // Helper của WriteExperiment (cùng lớp): thống kê từng cột, báo cả số mẫu và số ca bỏ qua.
    private static void WriteMetricTable(
        StreamWriter writer,
        CsvTable table,
        string size,
        List<string> variants,
        string[] columns,
        string[] labels)
    {
        List<string[]> lines = [["Biến", "n", "Skip", .. labels]];
        for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            var groupRows = GetGroupRows(table, size, variants[variantIndex]);
            var measuredCount = 0;
            foreach (var row in groupRows)
            {
                if (row[3] == "ok")
                {
                    measuredCount++;
                }
            }

            var cells = new List<string>
            {
                "V" + CsvTable.Number(variantIndex + 1),
                CsvTable.Number(measuredCount),
                CsvTable.Number(groupRows.Count - measuredCount),
            };
            foreach (var column in columns)
            {
                var columnIndex = Array.IndexOf(table.Headers, column);
                var samples = new List<double>();
                foreach (var row in groupRows)
                {
                    if (row[3] == "ok" && row[columnIndex].Length > 0)
                    {
                        samples.Add(double.Parse(row[columnIndex], CultureInfo.InvariantCulture));
                    }
                }

                var format = column is "Objective" or "WeightedObjective" or "PenalizedObjective" ? "F4" : "F3";
                if (column is "ElapsedMs" or "ConflictCount" or "WastedSeats" or "PlacedCount" or "ViolationCount" or "FailedCount" or "DisruptedCount")
                {
                    format = "F1";
                }

                cells.Add(FormatStatistics(samples, format));
            }

            lines.Add(cells.ToArray());
        }

        WriteMarkdownTable(writer, lines);
    }

    // Helper của WriteExperiment (cùng lớp): ghép cùng seed để tính giảm C, không lấy tỷ lệ của hai trung bình.
    private static void WriteReductions(StreamWriter writer, CsvTable table, string size, List<string> variants)
    {
        List<string[]> lines = [["Biến", "% giảm C so với M1", "% giảm C so với M2"]];
        string[] baselineNames = ["M1", "M2"];
        for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            var cells = new List<string> { "V" + CsvTable.Number(variantIndex + 1) };
            foreach (var baselineName in baselineNames)
            {
                var baselineBySeed = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (var row in GetGroupRows(table, size, baselineName))
                {
                    if (row[3] == "ok")
                    {
                        baselineBySeed.Add(row[1], double.Parse(row[7], CultureInfo.InvariantCulture));
                    }
                }

                var reductions = new List<double>();
                foreach (var row in GetGroupRows(table, size, variants[variantIndex]))
                {
                    if (row[3] != "ok")
                    {
                        continue;
                    }

                    if (!baselineBySeed.TryGetValue(row[1], out var baselineConflict) || baselineConflict == 0)
                    {
                        reductions.Clear();
                        break;
                    }

                    var conflictCount = double.Parse(row[7], CultureInfo.InvariantCulture);
                    reductions.Add(100 * (baselineConflict - conflictCount) / baselineConflict);
                }

                cells.Add(FormatStatistics(reductions, "F1"));
            }

            lines.Add(cells.ToArray());
        }

        WriteMarkdownTable(writer, lines);
    }

    // ---------- GOM NHÓM VÀ ĐỊNH DẠNG ----------
    // Helper của WriteMetricTable / WriteReductions (cùng lớp): giữ các dòng của một quy mô và biến.
    private static List<string[]> GetGroupRows(CsvTable table, string size, string variant)
    {
        var groupRows = new List<string[]>();
        foreach (var row in table.Rows)
        {
            if (row[0] == size && row[2] == variant)
            {
                groupRows.Add(row);
            }
        }

        return groupRows;
    }

    // Helper của WriteMetricTable / WriteReductions (cùng lớp): tính trung bình và độ lệch chuẩn mẫu hai lượt.
    private static string FormatStatistics(List<double> samples, string format)
    {
        if (samples.Count == 0)
        {
            return "n/a";
        }

        var sum = 0.0;
        foreach (var sample in samples)
        {
            sum += sample;
        }

        var mean = sum / samples.Count;
        var sumSquaredDifferences = 0.0;
        foreach (var sample in samples)
        {
            var difference = sample - mean;
            sumSquaredDifferences += difference * difference;
        }

        var standardDeviation = samples.Count > 1 ? Math.Sqrt(sumSquaredDifferences / (samples.Count - 1)) : 0;
        return mean.ToString(format, CultureInfo.InvariantCulture) + " ± " + standardDeviation.ToString(format, CultureInfo.InvariantCulture);
    }

    // Helper của WriteMetricTable / WriteReductions (cùng lớp): căn cột để đọc được cả Markdown thô.
    private static void WriteMarkdownTable(StreamWriter writer, List<string[]> lines)
    {
        var widths = new int[lines[0].Length];
        foreach (var line in lines)
        {
            for (var columnIndex = 0; columnIndex < line.Length; columnIndex++)
            {
                widths[columnIndex] = Math.Max(3, Math.Max(widths[columnIndex], line[columnIndex].Length));
            }
        }

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            writer.Write('|');
            for (var columnIndex = 0; columnIndex < widths.Length; columnIndex++)
            {
                writer.Write(' ');
                writer.Write(lines[lineIndex][columnIndex].PadRight(widths[columnIndex]));
                writer.Write(" |");
            }

            writer.WriteLine();
            if (lineIndex == 0)
            {
                writer.Write('|');
                foreach (var width in widths)
                {
                    writer.Write(new string('-', width + 2));
                    writer.Write('|');
                }

                writer.WriteLine();
            }
        }

        writer.WriteLine();
    }
}
