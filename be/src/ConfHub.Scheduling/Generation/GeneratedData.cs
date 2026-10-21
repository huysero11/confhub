using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Generation;

// Kết quả của bộ sinh: bài toán và cụm chủ đề của từng phiên (mã phiên -> số thứ tự cụm).
// Cụm chủ đề dùng để test "có cụm" và để tính chỉ số về chủ đề ở task sau.
public sealed record GeneratedData(SchedulingProblem Problem, IReadOnlyDictionary<int, int> TopicClusterBySession);
