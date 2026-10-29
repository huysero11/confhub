using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm Q1 theo ví dụ 6.5 và thứ tự nhập khi đóng góp bằng nhau.
public class ConflictContributionTests
{
    // ---------- VÍ DỤ 6.5 ----------
    // Dùng ở: dotnet test (đối chiếu đóng góp trước và sau C ↔ B).
    [Fact]
    public void ExampleContributionsAndRankingMatchManualCalculation()
    {
        var problem = ExampleProblem.Create();
        var greedySchedule = ExampleProblem.CreateGreedySchedule();
        var contributions = ConflictContribution.Calculate(problem, greedySchedule);
        Assert.Equal(35, contributions[ExampleProblem.SessionA]);
        Assert.Equal(35, contributions[ExampleProblem.SessionC]);
        Assert.Equal(0, contributions[ExampleProblem.SessionB]);
        Assert.Equal(0, contributions[ExampleProblem.SessionD]);
        Assert.Equal([ExampleProblem.SessionA, ExampleProblem.SessionC], ConflictContribution.RankCandidates(problem, greedySchedule, 3));
        Assert.Equal([ExampleProblem.SessionA], ConflictContribution.RankCandidates(problem, greedySchedule, 1));

        var swappedContributions = ConflictContribution.Calculate(problem, ExampleProblem.CreateSwappedSchedule());
        Assert.Equal(5, swappedContributions[ExampleProblem.SessionA]);
        Assert.Equal(5, swappedContributions[ExampleProblem.SessionB]);
        Assert.Equal(0, swappedContributions[ExampleProblem.SessionC]);
        Assert.Equal(0, swappedContributions[ExampleProblem.SessionD]);
    }

    // ---------- THỨ TỰ VÀ KHOẢNG THỜI GIAN ----------
    // Dùng ở: dotnet test (thứ tự đặt trong lịch không được thay thứ tự nhập khi phá hòa).
    [Fact]
    public void TiesFollowInputOrderEvenWhenScheduleOrderChanges()
    {
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionC, ExampleProblem.NineOClock, ExampleProblem.RoomP102);
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);
        Assert.Equal([ExampleProblem.SessionA, ExampleProblem.SessionC], ConflictContribution.RankCandidates(ExampleProblem.Create(), schedule, 3));
        Assert.Equal(2, ConflictContribution.Calculate(ExampleProblem.Create(), schedule).Count);
    }

    // Dùng ở: dotnet test (chồng khoảng dù khác mốc bắt đầu; chạm đầu mút và khác ngày không cộng W).
    [Fact]
    public void ContributionsUseIntervalsAndDaysInsteadOfStartSlots()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 120), TestData.Session(2, 60), TestData.Session(3, 60), TestData.Session(4, 60)],
            [TestData.Room(1, 10), TestData.Room(2, 10)],
            [TestData.Day(540, 720), TestData.Day(540, 720)],
            interests: [new InterestEntry(1, 1), new InterestEntry(1, 2), new InterestEntry(1, 3), new InterestEntry(1, 4)],
            slotMinutes: 60);
        var schedule = new Schedule();
        schedule.Place(1, 0, 1);
        schedule.Place(2, 1, 2);
        schedule.Place(3, 2, 1);
        schedule.Place(4, 3, 1);
        var contributions = ConflictContribution.Calculate(problem, schedule);
        Assert.Equal(1, contributions[1]);
        Assert.Equal(1, contributions[2]);
        Assert.Equal(0, contributions[3]);
        Assert.Equal(0, contributions[4]);
    }

    // Dùng ở: dotnet test (lịch rỗng không có ứng viên; count sai phải bị từ chối).
    [Fact]
    public void EmptyScheduleAndInvalidCountAreHandled()
    {
        var problem = ExampleProblem.Create();
        Assert.Empty(ConflictContribution.Calculate(problem, new Schedule()));
        Assert.Empty(ConflictContribution.RankCandidates(problem, new Schedule(), 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConflictContribution.RankCandidates(problem, new Schedule(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConflictContribution.RankCandidates(problem, new Schedule(), -1));
    }
}
