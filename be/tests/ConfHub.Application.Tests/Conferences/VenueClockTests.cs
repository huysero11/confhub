using ConfHub.Application.Common.Time;

namespace ConfHub.Application.Tests.Conferences;

public class VenueClockTests
{
    // Ngày tại Việt Nam đã sang hôm sau khi UTC vẫn còn ở ngày trước.
    [Fact]
    public void TodayUsesVenueDateAcrossUtcMidnight()
    {
        var utcNow = new DateTimeOffset(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);
        var clock = new VenueClock(new FixedTimeProvider(utcNow), ConferenceTestData.TimeZone);

        Assert.Equal(new DateOnly(2026, 11, 6), clock.Today);
        Assert.Equal(utcNow.UtcDateTime, clock.UtcNow);
        Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        // Trả mốc cố định để test không phụ thuộc giờ thật.
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
