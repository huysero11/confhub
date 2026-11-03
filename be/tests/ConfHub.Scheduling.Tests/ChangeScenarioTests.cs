using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm kịch bản chỉ thay phần được yêu cầu và không làm mất dữ liệu nguyện vọng.
public class ChangeScenarioTests
{
    // ---------- MẤT PHÒNG ----------
    // Phòng chỉ bị xóa ở bài toán mới, W và mọi dữ liệu khác giữ nguyên.
    [Fact]
    public void RemoveRoomKeepsOtherDataAndOriginalProblem()
    {
        var data = ProblemGenerator.Generate(GeneratorOptions.Small);
        var removedRoom = data.Problem.Rooms[0];
        var changedProblem = ChangeScenario.RemoveRoom(data, removedRoom.Id);
        Assert.Equal(data.Problem.Rooms.Count - 1, changedProblem.Rooms.Count);
        Assert.DoesNotContain(changedProblem.Rooms, room => room.Id == removedRoom.Id);
        Assert.Contains(removedRoom, data.Problem.Rooms);
        Assert.Equal(data.Problem.SpeakerAvailabilities, changedProblem.SpeakerAvailabilities);
        AssertUnchangedCore(data.Problem, changedProblem);
    }

    // Không bỏ qua mã phòng lạ; bài toán vẫn phải có ít nhất một phòng.
    [Fact]
    public void MissingOrLastRoomCannotBeRemoved()
    {
        var data = CreateData([]);
        Assert.Throws<ArgumentException>(() => ChangeScenario.RemoveRoom(data, 99));
        Assert.Throws<ArgumentException>(() => ChangeScenario.RemoveRoom(data, 1));
    }

    // ---------- GIỜ RẢNH DIỄN GIẢ ----------
    // Giữ nguyên mọi khoảng cũ ở ngày khác, không đổi giờ rảnh của diễn giả khác.
    [Fact]
    public void DeclaredAvailabilityOnlyLosesRequestedDay()
    {
        var data = CreateData([
            new SpeakerAvailability(10, 0, 540, 600),
            new SpeakerAvailability(10, 1, 600, 660),
            new SpeakerAvailability(10, 1, 720, 780),
            new SpeakerAvailability(20, 0, 540, 660),
        ]);
        var changedProblem = ChangeScenario.MakeSpeakerUnavailable(data, 10, 0);
        Assert.Equal(3, changedProblem.SpeakerAvailabilities.Count);
        Assert.Equal(
        [
            new SpeakerAvailability(10, 1, 600, 660),
            new SpeakerAvailability(10, 1, 720, 780),
            new SpeakerAvailability(20, 0, 540, 660),
        ],
        changedProblem.SpeakerAvailabilities);
        Assert.Equal(4, data.Problem.SpeakerAvailabilities.Count);
        Assert.Equal(data.Problem.Rooms, changedProblem.Rooms);
        AssertUnchangedCore(data.Problem, changedProblem);
    }

    // Diễn giả chưa khai được khai tường minh cả ngày còn lại.
    [Fact]
    public void UndeclaredAvailabilityBecomesAllOtherDays()
    {
        var data = CreateData([]);
        var changedProblem = ChangeScenario.MakeSpeakerUnavailable(data, 10, 0);
        Assert.Equal(new SpeakerAvailability(10, 1, 540, 780), Assert.Single(changedProblem.SpeakerAvailabilities));
        Assert.Empty(data.Problem.SpeakerAvailabilities);
        foreach (var slotIndex in changedProblem.GetValidStartSlots(1))
        {
            Assert.Equal(1, changedProblem.Timeline.Slots[slotIndex].DayIndex);
        }

        AssertUnchangedCore(data.Problem, changedProblem);
    }

    // Không thể dùng danh sách rỗng để biểu diễn diễn giả bận hết mọi ngày.
    [Fact]
    public void NoRemainingAvailabilityAndInvalidIdentifiersAreRejected()
    {
        var declaredData = CreateData([new SpeakerAvailability(10, 0, 540, 660)]);
        Assert.Throws<ArgumentException>(() => ChangeScenario.MakeSpeakerUnavailable(declaredData, 10, 0));
        var oneDayProblem = TestData.CreateProblem([TestData.Session(1, 60, 10)], [TestData.Room(1, 100)], [TestData.Day(540, 660)]);
        var oneDayData = new GeneratedData(oneDayProblem, new Dictionary<int, int>()) { Interests = [new InterestEntry(1, 1)] };
        Assert.Throws<ArgumentException>(() => ChangeScenario.MakeSpeakerUnavailable(oneDayData, 10, 0));

        // Dữ liệu không kèm nguyện vọng thì không dựng lại được ma trận quan tâm.
        var dataWithoutInterests = new GeneratedData(oneDayProblem, new Dictionary<int, int>());
        Assert.Throws<ArgumentException>(() => ChangeScenario.RemoveRoom(dataWithoutInterests, 1));
        Assert.Throws<ArgumentException>(() => ChangeScenario.MakeSpeakerUnavailable(declaredData, 99, 0));
        Assert.Throws<ArgumentException>(() => ChangeScenario.MakeSpeakerUnavailable(declaredData, 10, -1));
        Assert.Throws<ArgumentException>(() => ChangeScenario.MakeSpeakerUnavailable(declaredData, 10, 2));
    }

    // ---------- HỖ TRỢ DỮ LIỆU ----------
    // Helper của các test (cùng lớp): hai ngày, hai diễn giả, có nguyện vọng để so W.
    private static GeneratedData CreateData(IReadOnlyList<SpeakerAvailability> availabilities)
    {
        List<InterestEntry> interests = [new(1, 1), new(1, 2)];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10), TestData.Session(2, 60, 20)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 780), TestData.Day(540, 780)],
            availabilities,
            interests,
            60);
        return new GeneratedData(problem, new Dictionary<int, int>()) { Interests = interests };
    }

    // Helper của các test (cùng lớp): so dữ liệu thời gian, phiên và mọi phần tử ma trận quan tâm.
    private static void AssertUnchangedCore(SchedulingProblem originalProblem, SchedulingProblem changedProblem)
    {
        Assert.Equal(originalProblem.Sessions, changedProblem.Sessions);
        Assert.Equal(originalProblem.Days, changedProblem.Days);
        Assert.Equal(originalProblem.SlotMinutes, changedProblem.SlotMinutes);
        foreach (var firstSession in originalProblem.Sessions)
        {
            foreach (var secondSession in originalProblem.Sessions)
            {
                Assert.Equal(originalProblem.InterestMatrix.Get(firstSession.Id, secondSession.Id), changedProblem.InterestMatrix.Get(firstSession.Id, secondSession.Id));
            }
        }
    }
}
