using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Generation;

// Kết quả của bộ sinh: bài toán và cụm chủ đề của từng phiên (mã phiên -> số thứ tự cụm).
// Giữ nguyện vọng để ChangeScenario dựng lại ma trận quan tâm sau biến động.
public sealed record GeneratedData(SchedulingProblem Problem, IReadOnlyDictionary<int, int> TopicClusterBySession)
{
    public IReadOnlyList<InterestEntry> Interests { get; init; } = [];
}
