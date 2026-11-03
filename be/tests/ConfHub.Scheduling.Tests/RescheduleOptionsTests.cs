using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Rescheduling;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm tùy chọn xếp lại và việc constructor kiểm tra cả tùy chọn leo đồi.
public class RescheduleOptionsTests
{
    // Dùng ở: dotnet test (mặc định đúng 0,5 và cho phép tắt phạt bằng 0).
    [Fact]
    public void DefaultsAndZeroWeightAreValid()
    {
        var options = new RescheduleOptions();
        Assert.Equal(0.5, options.DisruptionWeight);
        Assert.Equal(new ImprovementOptions(), options.Improvement);
        options.Validate();
        (options with { DisruptionWeight = 0 }).Validate();
    }

    // Dùng ở: dotnet test (chặn trọng số sai trước khi thuật toán chạy).
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidWeightIsRejected(double weight)
    {
        var options = new RescheduleOptions { DisruptionWeight = weight };
        Assert.Throws<ArgumentException>(options.Validate);
        Assert.Throws<ArgumentException>(() => new Rescheduler(ExampleProblem.Create(), options));
    }

    // Dùng ở: dotnet test (kiểm lồng Improvement, kể cả null được truyền từ tầng gọi).
    [Fact]
    public void InvalidImprovementIsRejected()
    {
        var options = new RescheduleOptions { Improvement = new ImprovementOptions { CandidateCount = 0 } };
        Assert.Throws<ArgumentException>(options.Validate);
        Assert.Throws<ArgumentException>(() => new Rescheduler(ExampleProblem.Create(), options));
        Assert.Throws<ArgumentNullException>(() => new RescheduleOptions { Improvement = null! }.Validate());
    }
}
