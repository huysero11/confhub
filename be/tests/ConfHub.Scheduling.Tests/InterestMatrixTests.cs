using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

public class InterestMatrixTests
{
    [Fact]
    public void ExampleMatchesHandCalculation()
    {
        var matrix = ExampleProblem.Create().InterestMatrix;

        Assert.Equal(35, matrix.Get(ExampleProblem.SessionA, ExampleProblem.SessionC));
        Assert.Equal(5, matrix.Get(ExampleProblem.SessionA, ExampleProblem.SessionB));
        Assert.Equal(0, matrix.Get(ExampleProblem.SessionB, ExampleProblem.SessionC));
        Assert.Equal(0, matrix.Get(ExampleProblem.SessionD, ExampleProblem.SessionA));
    }

    [Fact]
    public void MatrixIsSymmetric()
    {
        var matrix = ExampleProblem.Create().InterestMatrix;

        Assert.Equal(
            matrix.Get(ExampleProblem.SessionA, ExampleProblem.SessionC),
            matrix.Get(ExampleProblem.SessionC, ExampleProblem.SessionA));
    }

    [Fact]
    public void DiagonalIsNumberOfInterestedAttendees()
    {
        var matrix = ExampleProblem.Create().InterestMatrix;

        Assert.Equal(40, matrix.Get(ExampleProblem.SessionA, ExampleProblem.SessionA));
        Assert.Equal(35, matrix.Get(ExampleProblem.SessionC, ExampleProblem.SessionC));
        Assert.Equal(5, matrix.Get(ExampleProblem.SessionB, ExampleProblem.SessionB));
        Assert.Equal(0, matrix.Get(ExampleProblem.SessionD, ExampleProblem.SessionD));
    }

    [Fact]
    public void TotalCoInterestSumsAllPairs()
    {
        var matrix = ExampleProblem.Create().InterestMatrix;

        Assert.Equal(40, matrix.TotalCoInterest);
    }

    [Fact]
    public void DuplicateEntriesAreCountedOnce()
    {
        List<InterestEntry> interests =
        [
            new InterestEntry(1, 1),
            new InterestEntry(1, 1),
            new InterestEntry(1, 2),
            new InterestEntry(1, 2),
        ];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 660)],
            null,
            interests);

        Assert.Equal(1, problem.InterestMatrix.Get(1, 2));
        Assert.Equal(1, problem.InterestMatrix.Get(1, 1));
        Assert.Equal(1, problem.InterestMatrix.TotalCoInterest);
    }

    [Fact]
    public void UnknownSessionIdIsRejected()
    {
        var matrix = ExampleProblem.Create().InterestMatrix;

        Assert.Throws<ArgumentException>(() => matrix.Get(99, ExampleProblem.SessionA));
    }
}
