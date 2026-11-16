namespace ConfHub.Application.Common.Time;

public sealed class VenueClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
{
    public TimeZoneInfo TimeZone { get; } = timeZone;
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZone));
}
