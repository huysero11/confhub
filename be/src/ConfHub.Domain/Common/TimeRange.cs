using ConfHub.Domain.Conferences;

namespace ConfHub.Domain.Common;

public sealed record TimeRange
{
    public const int StepMinutes = 15;

    private TimeRange()
    {
    }

    public TimeOnly Start { get; private init; }
    public TimeOnly End { get; private init; }
    public int DurationMinutes => (int)(End - Start).TotalMinutes;

    public static TimeRange Create(TimeOnly start, TimeOnly end)
    {
        if (start >= end || !IsQuarterHour(start) || !IsQuarterHour(end))
        {
            throw new DomainException(ConferenceErrorCodes.InvalidTimeRange, "Invalid quarter-hour time range.");
        }

        return new TimeRange { Start = start, End = end };
    }

    // Kiểm tra xem thời gian có phải là bội số của 15 phút hay không.
    public static bool IsQuarterHour(TimeOnly time)
    {
        var stepTicks = TimeSpan.TicksPerMinute * StepMinutes;
        return time.Ticks % stepTicks == 0;
    }

    public bool Overlaps(TimeRange other)
    {
        return Start < other.End && End > other.Start;
    }

    public bool Contains(TimeRange other)
    {
        return Start <= other.Start && End >= other.End;
    }
}
