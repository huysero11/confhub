namespace ConfHub.Scheduling.Objective;

// Trọng số của hàm mục tiêu f = Conflict * Cn + Load * Bn + Waste * Sn.
// Mỗi trọng số >= 0 và tổng bằng 1.
public sealed record ObjectiveWeights
{
    private const double SumTolerance = 1e-6;

    // Dùng ở: tầng gọi muốn đổi trọng số (T2.4 phân tích độ nhạy), test.
    public ObjectiveWeights(double conflict, double load, double waste)
    {
        if (double.IsNaN(conflict) || double.IsNaN(load) || double.IsNaN(waste))
        {
            throw new ArgumentException("Trọng số không phải là số.");
        }

        if (conflict < 0 || load < 0 || waste < 0)
        {
            throw new ArgumentException("Trọng số không được âm.");
        }

        if (Math.Abs((conflict + load + waste) - 1.0) > SumTolerance)
        {
            throw new ArgumentException("Tổng ba trọng số phải bằng 1.");
        }

        Conflict = conflict;
        Load = load;
        Waste = waste;
    }

    // Trọng số mặc định 0,7 / 0,2 / 0,1. Dùng ở: T2.2 / T2.3, T2.4, test.
    public static ObjectiveWeights Default { get; } = new(0.7, 0.2, 0.1);

    public double Conflict { get; }

    public double Load { get; }

    public double Waste { get; }
}
