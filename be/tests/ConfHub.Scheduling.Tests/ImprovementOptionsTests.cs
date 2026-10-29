using ConfHub.Scheduling.Improvement;
using ConfHub.Scheduling.Methods;
using ConfHub.Scheduling.Objective;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Kiểm mọi điều kiện của tùy chọn và việc kiểm tra ngay khi tạo thuật toán.
public class ImprovementOptionsTests
{
    // ---------- MẶC ĐỊNH ----------
    // Dùng ở: dotnet test (giữ đúng các hằng số của đặc tả 3.1).
    [Fact]
    public void DefaultsAreValidAndMatchSpecification()
    {
        var options = new ImprovementOptions();
        options.Validate();
        Assert.Equal(3, options.CandidateCount);
        Assert.Equal(200, options.MaxIterations);
        Assert.Equal(10, options.RestartCount);
        Assert.Equal(20, options.StepsPerTemperature);
        Assert.Equal(0.05, options.InitialTemperature);
        Assert.Equal(0.95, options.CoolingRate);
        Assert.Equal(1e-4, options.MinTemperature);
        (options with { Seed = int.MinValue }).Validate();
    }

    // ---------- ĐIỀU KIỆN SAI ----------
    // Dùng ở: dotnet test (từng số đếm không dương đều bị từ chối).
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveCountsAreRejected(int invalidCount)
    {
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { CandidateCount = invalidCount }.Validate());
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { MaxIterations = invalidCount }.Validate());
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { RestartCount = invalidCount }.Validate());
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { StepsPerTemperature = invalidCount }.Validate());
    }

    // Dùng ở: dotnet test (NaN/vô cực không được lọt qua các phép so sánh nhiệt độ).
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidTemperaturesAreRejected(double invalidTemperature)
    {
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { InitialTemperature = invalidTemperature }.Validate());
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { MinTemperature = invalidTemperature }.Validate());
    }

    // Dùng ở: dotnet test (hệ số hạ nhiệt phải nằm hẳn trong khoảng 0–1).
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidCoolingRatesAreRejected(double invalidRate)
    {
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { CoolingRate = invalidRate }.Validate());
    }

    // Dùng ở: dotnet test (cận nhiệt và tất cả điểm vào đều kiểm tùy chọn).
    [Fact]
    public void ReversedTemperatureBoundsAndInvalidConstructorOptionsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { MinTemperature = 0.05 }.Validate());
        Assert.Throws<ArgumentException>(() => new ImprovementOptions { MinTemperature = 0.06 }.Validate());
        var problem = ExampleProblem.Create();
        var options = new ImprovementOptions { MaxIterations = 0 };
        Assert.Throws<ArgumentException>(() => new LocalSearch(problem, ObjectiveWeights.Default, options));
        Assert.Throws<ArgumentException>(() => new SimulatedAnnealing(problem, ObjectiveWeights.Default, options));
        Assert.Throws<ArgumentException>(() => new LocalSearchScheduler(problem, options));
        Assert.Throws<ArgumentException>(() => new RestartScheduler(problem, options));
        Assert.Throws<ArgumentException>(() => new AnnealingScheduler(problem, options));
    }
}
