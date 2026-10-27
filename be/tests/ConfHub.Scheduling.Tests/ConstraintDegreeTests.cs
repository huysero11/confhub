using ConfHub.Scheduling.Greedy;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm công thức độ ràng buộc và thứ tự ổn định khi hòa.
public class ConstraintDegreeTests
{
    // Dùng ở: dotnet test (đối chiếu thứ tự A, D, B, C của ví dụ 6.5).
    [Fact]
    public void ExampleDegreesAndOrderMatchSpecification()
    {
        var problem = ExampleProblem.Create();
        Assert.Equal(0, ConstraintDegree.CalculateDegree(problem, ExampleProblem.SessionA));
        Assert.Equal(0, ConstraintDegree.CalculateDegree(problem, ExampleProblem.SessionD));
        Assert.Equal(1, ConstraintDegree.CalculateDegree(problem, ExampleProblem.SessionB));
        Assert.Equal(1, ConstraintDegree.CalculateDegree(problem, ExampleProblem.SessionC));
        Assert.Equal([1, 2, 3, 4], ConstraintDegree.OrderSessions(problem));
    }

    // Dùng ở: dotnet test (không diễn giả thì trừ 0; hòa giữ thứ tự nhập không theo mã).
    [Fact]
    public void NoSpeakersSubtractsZeroAndTiesPreserveInputOrder()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(9, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        Assert.Equal(2, ConstraintDegree.CalculateDegree(problem, 9));
        Assert.Equal([9, 2], ConstraintDegree.OrderSessions(problem));
    }

    // Dùng ở: dotnet test (lấy số phiên lớn nhất của một diễn giả, không cộng hai người).
    [Fact]
    public void MultipleSpeakersUseBusiestSpeaker()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 10, 20), TestData.Session(2, 60, 20), TestData.Session(3, 60, 20)],
            [TestData.Room(1, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 60);
        Assert.Equal(-1, ConstraintDegree.CalculateDegree(problem, 1));
    }
}
