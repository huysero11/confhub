namespace ConfHub.Scheduling.Problem;

// Một phiên cần xếp. SpeakerIds là các diễn giả đã nhận lời (có thể rỗng).
// ExpectedSize null thì quy mô phiên được ước lượng từ số người quan tâm.
public sealed record SchedulingSession(
    int Id,
    string Name,
    int DurationMinutes,
    IReadOnlyList<int> SpeakerIds,
    int? ExpectedSize);
