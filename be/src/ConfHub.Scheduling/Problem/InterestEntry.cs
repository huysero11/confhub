namespace ConfHub.Scheduling.Problem;

// Một người tham dự quan tâm một phiên. Cặp trùng chỉ được tính một lần.
public sealed record InterestEntry(int AttendeeId, int SessionId);
