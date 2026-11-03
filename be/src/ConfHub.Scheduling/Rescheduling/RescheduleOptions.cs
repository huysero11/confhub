using ConfHub.Scheduling.Improvement;

namespace ConfHub.Scheduling.Rescheduling;

// Tùy chọn xếp lại: mức phạt xáo trộn và giới hạn leo đồi dùng chung với T2.3.
public sealed record RescheduleOptions
{
    public double DisruptionWeight { get; init; } = 0.5;

    public ImprovementOptions Improvement { get; init; } = new();

    // Loại tùy chọn sai trước khi chạy.
    public void Validate()
    {
        if (!double.IsFinite(DisruptionWeight) || DisruptionWeight < 0)
        {
            throw new ArgumentException("Trọng số xáo trộn phải hữu hạn và không âm.", nameof(DisruptionWeight));
        }

        ArgumentNullException.ThrowIfNull(Improvement);
        Improvement.Validate();
    }
}
