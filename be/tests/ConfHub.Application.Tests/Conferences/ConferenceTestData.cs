using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Tests.Conferences;

internal static class ConferenceTestData
{
    public static DateOnly Today { get; } = new(2026, 11, 5);
    public static DateTime NowUtc { get; } = new(2026, 11, 5, 3, 0, 0, DateTimeKind.Utc);
    public static DateOnly FirstDate { get; } = new(2026, 12, 3);
    public static TimeZoneInfo TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public static TimeRange Hours => TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(17, 0));
    public static ConferenceDetails Details => new(
        "  Công nghệ Xanh 2026  ",
        " Green Technology 2026 ",
        " Mô tả ",
        " Description ",
        " Hà Nội ",
        new DateTime(2026, 12, 1, 16, 59, 0, DateTimeKind.Utc),
        true);

    // Dựng hội nghị có hai ngày và chủ sở hữu riêng cho mỗi test.
    public static Conference Create(int dayCount = 2)
    {
        return Conference.Create(
            Guid.NewGuid(),
            Details,
            FirstDate,
            FirstDate.AddDays(dayCount - 1),
            Hours,
            Today,
            NowUtc,
            TimeZone);
    }
}
