using ConfHub.Scheduling.Constraints;

namespace ConfHub.Scheduling.Rescheduling;

// Phiên bị gỡ ở bước giữ lịch; lý do vẫn được trả kể cả khi đã xếp lại thành công.
public sealed record DisplacedSession(int SessionId, IReadOnlyList<ConstraintCode> Codes, string Reason);
