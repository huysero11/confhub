using ConfHub.Scheduling.Rooms;

namespace ConfHub.Scheduling.Tests;

// Kiểm phân công tối ưu, ma trận chữ nhật và ô cấm độc lập với thuật toán xếp lịch.
public class HungarianAlgorithmTests
{
    // Dùng ở: dotnet test (đối chiếu nhóm A, C của ví dụ 6.5).
    [Fact]
    public void ExampleAssignsAToP101AndCToP102WithCost77()
    {
        long[][] costs = [[58, HungarianAlgorithm.Infeasible], [79, 19]];
        var assignment = HungarianAlgorithm.Solve(costs);
        Assert.Equal([0, 1], assignment);
        Assert.Equal(77, costs[0][assignment[0]] + costs[1][assignment[1]]);
    }

    // Dùng ở: dotnet test (đáp án khác cách chọn cột rẻ nhất từng hàng).
    [Fact]
    public void ThreeByThreeFindsGlobalMinimum()
    {
        long[][] costs = [[4, 1, 3], [2, 0, 5], [3, 2, 2]];
        var assignment = HungarianAlgorithm.Solve(costs);
        Assert.Equal([1, 0, 2], assignment);
        Assert.Equal(5, costs[0][assignment[0]] + costs[1][assignment[1]] + costs[2][assignment[2]]);
    }

    // Dùng ở: dotnet test (số phòng nhiều hơn số phiên).
    [Fact]
    public void RectangularMatrixMayLeaveColumnsUnused()
    {
        Assert.Equal([2, 1], HungarianAlgorithm.Solve([[8, 7, 1], [6, 2, 5]]));
    }

    // Dùng ở: dotnet test (tránh ô cấm khi còn phép ghép khả thi).
    [Fact]
    public void ForbiddenCellsAreAvoidedWhenPossible()
    {
        Assert.Equal([1, 0], HungarianAlgorithm.Solve([[HungarianAlgorithm.Infeasible, 4], [2, 1]]));
    }

    // Dùng ở: dotnet test (trường hợp nhỏ nhất và ô cấm bắt buộc để người gọi phát hiện).
    [Fact]
    public void SingleCellAndEmptyMatrixAreSupported()
    {
        Assert.Equal([0], HungarianAlgorithm.Solve([[7]]));
        Assert.Equal([0], HungarianAlgorithm.Solve([[HungarianAlgorithm.Infeasible]]));
        Assert.Empty(HungarianAlgorithm.Solve([]));
    }

    // Dùng ở: dotnet test (đầu vào không đúng hợp đồng bị từ chối).
    [Fact]
    public void InvalidMatricesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => HungarianAlgorithm.Solve(null!));
        Assert.Throws<ArgumentException>(() => HungarianAlgorithm.Solve([[1], [2]]));
        Assert.Throws<ArgumentException>(() => HungarianAlgorithm.Solve([[1, 2], [3]]));
        Assert.Throws<ArgumentException>(() => HungarianAlgorithm.Solve([null!]));
        Assert.Throws<ArgumentException>(() => HungarianAlgorithm.Solve([[-1]]));
        Assert.Throws<ArgumentException>(() => HungarianAlgorithm.Solve([[long.MaxValue]]));
    }
}
