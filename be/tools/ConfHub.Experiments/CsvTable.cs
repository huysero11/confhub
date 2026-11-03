// Bảng kết quả trong bộ nhớ và bộ ghi CSV, dùng dấu chấm thập phân ở mọi máy.
using System.Globalization;
using System.Text;
using ConfHub.Scheduling.Evaluation;

namespace ConfHub.Experiments;

internal sealed class CsvTable
{
    // Khởi tạo các cột chỉ số chung, nối thêm chỉ số riêng của thực nghiệm.
    public CsvTable(string name, params string[] extraHeaders)
    {
        Name = name;
        Headers =
        [
            "Size", "Seed", "Variant", "Status", "SessionCount", "PlacedCount", "PlacementRate",
            "ConflictCount", "LoadStdDev", "WastedSeats", "Objective", "NoLossAttendeeRate",
            "RoomFillRate", "ViolationCount", "ElapsedMs", .. extraHeaders,
        ];
    }

    public string Name { get; }

    public string[] Headers { get; }

    public List<string[]> Rows { get; } = [];

    // ---------- ĐỊNH DẠNG SỐ ----------
    // Số giữ độ chính xác double; thời gian được làm tròn riêng tới 0,1 ms.
    public static string Number(double number)
    {
        return number.ToString("G17", CultureInfo.InvariantCulture);
    }

    // ---------- THÊM KẾT QUẢ ----------
    // Ghi một lần chạy; vi phạm cứng làm dừng thực nghiệm để không âm thầm xuất lịch sai.
    public void AddMetrics(string size, int seed, string variant, ScheduleMetrics metrics, double? elapsedMilliseconds, params double[] extraMetrics)
    {
        if (metrics.ViolationCount != 0)
        {
            throw new InvalidOperationException($"{Name}/{size}/{seed}/{variant}: có {metrics.ViolationCount} vi phạm.");
        }

        var row = new List<string>
        {
            size, Number(seed), variant, "ok", Number(metrics.SessionCount), Number(metrics.PlacedCount),
            Number(metrics.PlacementRate), Number(metrics.ConflictCount), Number(metrics.LoadStdDev),
            Number(metrics.WastedSeats), Number(metrics.Objective), Number(metrics.NoLossAttendeeRate),
            Number(metrics.RoomFillRate), Number(metrics.ViolationCount),
            elapsedMilliseconds?.ToString("F1", CultureInfo.InvariantCulture) ?? string.Empty,
        };
        foreach (var extraMetric in extraMetrics)
        {
            row.Add(Number(extraMetric));
        }

        if (row.Count != Headers.Length)
        {
            throw new ArgumentException("Số chỉ số không khớp tiêu đề CSV.", nameof(extraMetrics));
        }

        Rows.Add(row.ToArray());
    }

    // Ghi ca không có diễn giả hợp lệ có phiên ngày 0; để trống chỉ số thay vì giả số 0.
    public void AddSkipped(string size, int seed, string variant)
    {
        var row = new string[Headers.Length];
        Array.Fill(row, string.Empty);
        row[0] = size;
        row[1] = Number(seed);
        row[2] = variant;
        row[3] = "skipped";
        Rows.Add(row);
    }

    // ---------- XUẤT CSV ----------
    // Ghi UTF-8 và CRLF; trường chứa dấu phân cách hoặc dấu nháy được bao đúng chuẩn CSV.
    public void Write(string outputDirectory)
    {
        using var writer = new StreamWriter(Path.Combine(outputDirectory, Name + ".csv"), false, new UTF8Encoding(false));
        writer.NewLine = "\r\n";
        WriteRow(writer, Headers);
        foreach (var row in Rows)
        {
            WriteRow(writer, row);
        }
    }

    // Helper của Write (cùng lớp): thoát dấu nháy và phân tách từng ô.
    private static void WriteRow(StreamWriter writer, string[] cells)
    {
        for (var columnIndex = 0; columnIndex < cells.Length; columnIndex++)
        {
            if (columnIndex > 0)
            {
                writer.Write(',');
            }

            var cell = cells[columnIndex];
            if (cell.Contains(',') || cell.Contains('"') || cell.Contains('\r') || cell.Contains('\n'))
            {
                writer.Write('"');
                writer.Write(cell.Replace("\"", "\"\"", StringComparison.Ordinal));
                writer.Write('"');
            }
            else
            {
                writer.Write(cell);
            }
        }

        writer.WriteLine();
    }
}
