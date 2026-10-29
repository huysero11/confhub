namespace ConfHub.Scheduling.Improvement;

// Tham số dùng chung cho cải thiện cục bộ, khởi động lại và luyện kim.
// Các lớp chạy thuật toán kiểm tra tùy chọn ngay khi được tạo.
public sealed record ImprovementOptions
{
    // ---------- GIỚI HẠN TÌM KIẾM ----------
    public int CandidateCount { get; init; } = 3;

    public int MaxIterations { get; init; } = 200;

    public int RestartCount { get; init; } = 10;

    public int Seed { get; init; } = 1;

    // ---------- LỊCH NHIỆT ----------
    public double InitialTemperature { get; init; } = 0.05;

    public double CoolingRate { get; init; } = 0.95;

    public int StepsPerTemperature { get; init; } = 20;

    public double MinTemperature { get; init; } = 1e-4;

    // ---------- KIỂM TRA ----------
    // Dùng ở: LocalSearch, SimulatedAnnealing, RestartScheduler (constructor loại tùy chọn sai trước khi chạy).
    public void Validate()
    {
        if (CandidateCount <= 0 || MaxIterations <= 0 || RestartCount <= 0 || StepsPerTemperature <= 0)
        {
            throw new ArgumentException("Các số đếm phải lớn hơn 0.");
        }

        if (!double.IsFinite(InitialTemperature) || !double.IsFinite(MinTemperature)
            || InitialTemperature <= 0 || MinTemperature <= 0 || MinTemperature >= InitialTemperature)
        {
            throw new ArgumentException("Nhiệt độ phải hữu hạn và 0 < MinTemperature < InitialTemperature.");
        }

        if (!double.IsFinite(CoolingRate) || CoolingRate <= 0 || CoolingRate >= 1)
        {
            throw new ArgumentException("Hệ số hạ nhiệt phải hữu hạn và nằm giữa 0 và 1.");
        }
    }
}
