using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Improvement;

// Kết quả cải thiện cho T2.4: lịch, f, số vòng đã thử và số bước đã nhận.
// Luyện kim cộng số bước thử/nhận của nó với số vòng/bước nhận của cục bộ cuối.
public sealed record ImprovementResult(Schedule Schedule, double Value, int Iterations, int AcceptedMoves);
