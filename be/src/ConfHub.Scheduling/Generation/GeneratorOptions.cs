namespace ConfHub.Scheduling.Generation;

// Tùy chọn của bộ sinh dữ liệu mô phỏng. Giá trị mặc định chính là quy mô Small.
// Cùng Seed và cùng tùy chọn thì sinh ra đúng cùng một dữ liệu.
public sealed record GeneratorOptions
{
    // ---------- BA QUY MÔ DỰNG SẴN ----------
    public static GeneratorOptions Small => new();

    public static GeneratorOptions Medium => new()
    {
        SessionCount = 60,
        AttendeeCount = 600,
        DayCount = 3,
        RoomCount = 4,
        TopicClusterCount = 6,
    };

    public static GeneratorOptions Large => new()
    {
        SessionCount = 100,
        AttendeeCount = 1000,
        DayCount = 3,
        RoomCount = 7,
        TopicClusterCount = 8,
    };

    // ---------- QUY MÔ ----------
    public int Seed { get; init; } = 1;

    public int SessionCount { get; init; } = 30;

    public int AttendeeCount { get; init; } = 300;

    public int DayCount { get; init; } = 2;

    public int RoomCount { get; init; } = 3;

    public int TopicClusterCount { get; init; } = 5;

    // ---------- NGUYỆN VỌNG ----------
    public int MinInterestsPerAttendee { get; init; } = 3;

    public int MaxInterestsPerAttendee { get; init; } = 8;

    // Xác suất một lần chọn phiên lấy từ cụm chủ đề chính của người đó.
    public double HomeClusterProbability { get; init; } = 0.7;

    // ---------- PHIÊN VÀ DIỄN GIẢ ----------
    // Xác suất phiên dài 90 phút (ngược lại 60 phút).
    public double LongSessionProbability { get; init; } = 0.3;

    public double SecondSpeakerProbability { get; init; } = 0.2;

    // Tỉ lệ diễn giả (có phiên) khai giờ rảnh.
    public double ConstrainedSpeakerFraction { get; init; } = 0.3;
}
