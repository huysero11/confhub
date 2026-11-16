using ConfHub.Application.Conferences.AddConferenceDay;

namespace ConfHub.Application.Tests.Conferences;

public class AddConferenceDayCommandValidatorTests
{
    private readonly AddConferenceDayCommandValidator _validator = new();

    // Dữ liệu hợp lệ đi qua validator mà không cần đồng hồ hay CSDL.
    [Fact]
    public void ValidInputPasses()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    // Kiểm lỗi đầu vào của trường Date.
    [Fact]
    public void InvalidDateCase1IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Date = default });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Date");
    }

    // Kiểm lỗi đầu vào của trường StartTime.
    [Fact]
    public void InvalidStartTimeCase2IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { StartTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "StartTime");
    }

    // Kiểm lỗi đầu vào của trường EndTime.
    [Fact]
    public void InvalidEndTimeCase3IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndTime");
    }

    // Kiểm lỗi đầu vào của trường EndTime.
    [Fact]
    public void InvalidEndTimeCase4IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndTime = new TimeOnly(7, 0) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndTime");
    }

    // Kiểm lỗi đầu vào của trường EndTime.
    [Fact]
    public void InvalidEndTimeCase5IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndTime = new TimeOnly(8, 45) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndTime");
    }

    // Helper của các test validation (cùng lớp): Dữ liệu hợp lệ làm mốc.
    private static AddConferenceDayCommand Valid()
    {
        return new AddConferenceDayCommand(Guid.NewGuid(), new DateOnly(2026, 12, 3), new TimeOnly(8, 0), new TimeOnly(17, 0));
    }
}
