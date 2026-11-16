using ConfHub.Application.Conferences.AddBreakSlot;

namespace ConfHub.Application.Tests.Conferences;

public class AddBreakSlotCommandValidatorTests
{
    private readonly AddBreakSlotCommandValidator _validator = new();

    // Dữ liệu hợp lệ đi qua validator mà không cần đồng hồ hay CSDL.
    [Fact]
    public void ValidInputPasses()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    // Kiểm lỗi đầu vào của trường StartTime.
    [Fact]
    public void InvalidStartTimeCase1IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { StartTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "StartTime");
    }

    // Kiểm lỗi đầu vào của trường EndTime.
    [Fact]
    public void InvalidEndTimeCase2IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndTime");
    }

    // Kiểm lỗi đầu vào của trường EndTime.
    [Fact]
    public void InvalidEndTimeCase3IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndTime = new TimeOnly(7, 0) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndTime");
    }

    // Kiểm lỗi đầu vào của trường Name.
    [Fact]
    public void InvalidNameCase4IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Name = " " });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Name");
    }

    // Kiểm lỗi đầu vào của trường Name.
    [Fact]
    public void InvalidNameCase5IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Name = new string('a', 101) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Name");
    }

    // Helper của các test validation (cùng lớp): Dữ liệu hợp lệ làm mốc.
    private static AddBreakSlotCommand Valid()
    {
        return new AddBreakSlotCommand(Guid.NewGuid(), Guid.NewGuid(), "Nghỉ", new TimeOnly(11, 30), new TimeOnly(13, 0));
    }
}
