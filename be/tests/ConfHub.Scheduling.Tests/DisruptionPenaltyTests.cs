using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm D, n và ảnh hưởng của phiên mới / đã xóa, độc lập với thuật toán tìm lịch.
public class DisruptionPenaltyTests
{
    // ---------- ĐẾM VỊ TRÍ THAY ĐỔI ----------
    // Đổi mốc, đổi phòng và mất phiên là ba xáo trộn riêng.
    [Fact]
    public void IdenticalScheduleHasZeroDisruptionAndThreeChangesCountOnceEach()
    {
        var baseSchedule = ExampleProblem.CreateSwappedSchedule();
        var penalty = new DisruptionPenalty(ExampleProblem.Create(), baseSchedule, 0.5);
        Assert.Equal(4, penalty.BaseSessionCount);
        Assert.Equal(0, penalty.CountDisrupted(baseSchedule));
        Assert.Empty(penalty.GetDisruptedSessionIds(baseSchedule));

        var changedSchedule = baseSchedule.Clone();
        changedSchedule.Place(ExampleProblem.SessionA, 1, ExampleProblem.RoomP101);
        changedSchedule.SetRoom(ExampleProblem.SessionD, ExampleProblem.RoomP101);
        changedSchedule.Remove(ExampleProblem.SessionB);
        Assert.Equal(3, penalty.CountDisrupted(changedSchedule));
        Assert.Equal([1, 2, 3], penalty.GetDisruptedSessionIds(changedSchedule));
    }

    // Giao của bài toán mới và lịch gốc; danh sách theo thứ tự nhập mới.
    [Fact]
    public void AddedAndDeletedSessionsAreExcludedAndBaseIsSnapshotted()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(4, 60), TestData.Session(2, 60), TestData.Session(5, 60)],
            [TestData.Room(1, 100), TestData.Room(2, 40)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        var baseSchedule = ExampleProblem.CreateSwappedSchedule();
        var penalty = new DisruptionPenalty(problem, baseSchedule, 0.5);
        baseSchedule.Remove(4);
        var schedule = new Schedule();
        schedule.Place(5, 0, 1);
        Assert.Equal(2, penalty.BaseSessionCount);
        Assert.Equal(2, penalty.CountDisrupted(schedule));
        Assert.Equal([4, 2], penalty.GetDisruptedSessionIds(schedule));
    }

    // ---------- BIÊN VÀ ĐẦU VÀO ----------
    // N = 0 không chia cho 0 và không có phạt.
    [Fact]
    public void EmptyIntersectionHasZeroPenalty()
    {
        var problem = ExampleProblem.Create();
        var penalty = new DisruptionPenalty(problem, new Schedule(), 0.5);
        var schedule = ExampleProblem.CreateGreedySchedule();
        var objective = new ObjectiveCalculator(problem, ObjectiveWeights.Default, penalty).Evaluate(schedule);
        Assert.Equal(0, penalty.BaseSessionCount);
        Assert.Equal(0, objective.DisruptedCount);
        Assert.Equal(0, objective.NormalizedDisruption);
        Assert.Equal(new ObjectiveCalculator(problem, ObjectiveWeights.Default).Evaluate(schedule).Value, objective.Value);
    }

    // Không chấp nhận trọng số âm hoặc không hữu hạn.
    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidWeightsAreRejected(double weight)
    {
        Assert.Throws<ArgumentException>(() => new DisruptionPenalty(ExampleProblem.Create(), new Schedule(), weight));
    }
}
