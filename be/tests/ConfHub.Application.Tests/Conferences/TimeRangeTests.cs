using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Tests.Conferences;

public class TimeRangeTests
{
    // Bước phút, thứ tự và phần lẻ của giây đều phải hợp lệ.
    [Theory]
    [InlineData(8, 10, 0, 9, 0)]
    [InlineData(8, 0, 1, 9, 0)]
    [InlineData(9, 0, 0, 9, 0)]
    [InlineData(10, 0, 0, 9, 0)]
    [InlineData(23, 0, 0, 1, 0)]
    public void InvalidRangesAreRejected(int startHour, int startMinute, int startSecond, int endHour, int endMinute)
    {
        var exception = Assert.Throws<DomainException>(() => TimeRange.Create(
            new TimeOnly(startHour, startMinute, startSecond), new TimeOnly(endHour, endMinute)));
        Assert.Equal(ConferenceErrorCodes.InvalidTimeRange, exception.Code);
    }

    // Không chấp nhận một tick lẻ dù phần giây hiển thị bằng 0.
    [Fact]
    public void FractionalSecondIsRejected()
    {
        Assert.False(TimeRange.IsQuarterHour(new TimeOnly(8, 0).Add(TimeSpan.FromTicks(1))));
    }

    // Chạm đầu mút và rời nhau không phải chồng lấn; phép kiểm đối xứng.
    [Theory]
    [InlineData(8, 30, 9, 30, true)]
    [InlineData(9, 0, 10, 0, false)]
    [InlineData(10, 0, 11, 0, false)]
    [InlineData(7, 0, 8, 0, false)]
    public void OverlapUsesOpenBoundary(int startHour, int startMinute, int endHour, int endMinute, bool expected)
    {
        var first = TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(9, 0));
        var second = TimeRange.Create(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
        Assert.Equal(expected, first.Overlaps(second));
        Assert.Equal(expected, second.Overlaps(first));
    }

    // Value object so sánh theo giá trị và chứa cả hai đầu mút.
    [Fact]
    public void ContainsIncludesBoundariesAndValueEquality()
    {
        var hours = ConferenceTestData.Hours;
        Assert.True(hours.Contains(TimeRange.Create(hours.Start, hours.End)));
        Assert.True(hours.Contains(TimeRange.Create(new TimeOnly(9, 0), hours.End)));
        Assert.False(hours.Contains(TimeRange.Create(new TimeOnly(7, 0), hours.End)));
        Assert.Equal(540, hours.DurationMinutes);
        Assert.Equal(hours, ConferenceTestData.Hours);
    }
}
