using ConfHub.Application.Tracks.CreateTrack;

namespace ConfHub.Application.Tests.Tracks;

public class CreateTrackCommandValidatorTests
{
    private readonly CreateTrackCommandValidator _validator = new();

    // Dữ liệu hợp lệ đi qua validator mà không cần đồng hồ hay CSDL.
    [Fact]
    public void ValidInputPasses()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    // Kiểm lỗi đầu vào của trường NameVi.
    [Fact]
    public void InvalidNameViCase1IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { NameVi = " " });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "NameVi");
    }

    // Kiểm lỗi đầu vào của trường NameVi.
    [Fact]
    public void InvalidNameViCase2IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { NameVi = new string('a', 101) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "NameVi");
    }

    // Kiểm lỗi đầu vào của trường NameEn.
    [Fact]
    public void InvalidNameEnCase3IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { NameEn = new string('a', 101) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "NameEn");
    }

    // BR17: Tên tiếng Anh không được thiếu, rỗng hoặc chỉ có khoảng trắng.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void MissingNameEnIsRejected(string? nameEn)
    {
        var validationResult = _validator.Validate(Valid() with { NameEn = nameEn! });

        var failure = Assert.Single(validationResult.Errors);
        Assert.Equal("NameEn", failure.PropertyName);
        Assert.Equal("NameEnLength", failure.ErrorCode);
    }

    // BR17: Giới hạn tên tiếng Anh được tính sau khi cắt khoảng trắng hai đầu.
    [Fact]
    public void TrimmedNameEnAtMaximumLengthPasses()
    {
        var command = Valid() with { NameEn = " " + new string('a', 100) + " " };

        Assert.True(_validator.Validate(command).IsValid);
    }

    // Helper của các test validation (cùng lớp): Dữ liệu hợp lệ làm mốc.
    private static CreateTrackCommand Valid()
    {
        return new CreateTrackCommand(Guid.NewGuid(), "Công nghệ", "Technology");
    }
}
