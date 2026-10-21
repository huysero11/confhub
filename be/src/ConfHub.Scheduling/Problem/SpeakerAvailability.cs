namespace ConfHub.Scheduling.Problem;

// Một khoảng giờ rảnh diễn giả đã khai. Diễn giả không khai dòng nào thì được coi là rảnh mọi lúc.
public sealed record SpeakerAvailability(int SpeakerId, int DayIndex, int StartMinute, int EndMinute);
