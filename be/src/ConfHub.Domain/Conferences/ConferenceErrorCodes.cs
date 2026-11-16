namespace ConfHub.Domain.Conferences;

public static class ConferenceErrorCodes
{
    public const string InvalidTimeRange = "InvalidTimeRange";
    public const string DayDateTaken = "DayDateTaken";
    public const string TooManyDays = "TooManyDays";
    public const string DayInPast = "DayInPast";
    public const string LastDayRequired = "LastDayRequired";
    public const string RegistrationCloseAfterStart = "RegistrationCloseAfterStart";
    public const string RegistrationCloseInPast = "RegistrationCloseInPast";
    public const string BreakOutsideDay = "BreakOutsideDay";
    public const string BreakOverlap = "BreakOverlap";
    public const string TooManyTracks = "TooManyTracks";
    public const string RoomNameTaken = "RoomNameTaken";
    public const string TrackNameTaken = "TrackNameTaken";
}
