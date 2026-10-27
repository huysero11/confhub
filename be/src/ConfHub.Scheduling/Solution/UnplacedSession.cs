using ConfHub.Scheduling.Constraints;

namespace ConfHub.Scheduling.Solution;

/*
blockedSlotCounts = map lưu {mã ràng buộc, số mốc bị chặn bởi mã này}
*/
public sealed record UnplacedSession(
    int SessionId,
    IReadOnlyDictionary<ConstraintCode, int> BlockedSlotCounts,
    string Reason)
{
    // Dùng ở: GreedyScheduler.Run, FirstFitScheduler.Run (tạo lý do tiếng Việt cho phiên bị bỏ lại).
    public static UnplacedSession Create(int sessionId, IReadOnlyDictionary<ConstraintCode, int> blockedSlotCounts)
    {
        ArgumentNullException.ThrowIfNull(blockedSlotCounts);

        var counts = new Dictionary<ConstraintCode, int>(blockedSlotCounts); // copy từ dictionary gốc
        if (counts.Count == 1 && counts.ContainsKey(ConstraintCode.H5))
        {
            return new UnplacedSession(sessionId, counts, $"Phiên {sessionId} không có mốc bắt đầu hợp lệ (H5).");
        }

        var reasons = new List<string>();
        foreach (var constraintCode in Enum.GetValues<ConstraintCode>())
        {
            if (counts.TryGetValue(constraintCode, out var blockedCount))
            {
                var description = constraintCode switch
                {
                    ConstraintCode.H1 => "trùng diễn giả (H1)",
                    ConstraintCode.H2 => "hết phòng (H2)",
                    ConstraintCode.H3 => "trùng phòng (H3)",
                    ConstraintCode.H4 => "phòng không đủ sức chứa (H4)",
                    _ => "mốc không hợp lệ (H5)",
                };
                reasons.Add($"{description} ở {blockedCount} mốc");
            }
        }

        return new UnplacedSession(sessionId, counts, $"Phiên {sessionId} không xếp được: {string.Join(", ", reasons)}.");
    }
}
