using ConfHub.Application.Rooms.CreateRoom;

namespace ConfHub.Application.Tests.Rooms;

public class CreateRoomCommandValidatorTests
{
    private readonly CreateRoomCommandValidator _validator = new();

    // Dữ liệu hợp lệ đi qua validator mà không cần đồng hồ hay CSDL.
    [Fact]
    public void ValidInputPasses()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    // Kiểm lỗi đầu vào của trường Name.
    [Fact]
    public void InvalidNameCase1IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Name = " " });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Name");
    }

    // Kiểm lỗi đầu vào của trường Name.
    [Fact]
    public void InvalidNameCase2IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Name = new string('a', 51) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Name");
    }

    // Kiểm lỗi đầu vào của trường Capacity.
    [Fact]
    public void InvalidCapacityCase3IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Capacity = 0 });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Capacity" && failure.ErrorCode == "CapacityRange");
    }

    // Kiểm lỗi đầu vào của trường Capacity.
    [Fact]
    public void InvalidCapacityCase4IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Capacity = 10001 });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Capacity" && failure.ErrorCode == "CapacityRange");
    }

    // Helper của các test validation (cùng lớp): Dữ liệu hợp lệ làm mốc.
    private static CreateRoomCommand Valid()
    {
        return new CreateRoomCommand(Guid.NewGuid(), "P101", 100);
    }
}
