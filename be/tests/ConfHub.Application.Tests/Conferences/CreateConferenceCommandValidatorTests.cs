using ConfHub.Application.Conferences.CreateConference;

namespace ConfHub.Application.Tests.Conferences;

public class CreateConferenceCommandValidatorTests
{
    private readonly CreateConferenceCommandValidator _validator = new();

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
        var validationResult = _validator.Validate(Valid() with { NameVi = new string('a', 201) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "NameVi");
    }

    // Kiểm lỗi đầu vào của trường NameEn.
    [Fact]
    public void InvalidNameEnCase3IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { NameEn = new string('a', 201) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "NameEn");
    }

    // Kiểm lỗi đầu vào của trường DescriptionVi.
    [Fact]
    public void InvalidDescriptionViCase4IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DescriptionVi = new string('a', 4001) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DescriptionVi");
    }

    // Kiểm lỗi đầu vào của trường DescriptionEn.
    [Fact]
    public void InvalidDescriptionEnCase5IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DescriptionEn = new string('a', 4001) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DescriptionEn");
    }

    // Kiểm lỗi đầu vào của trường Venue.
    [Fact]
    public void InvalidVenueCase6IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Venue = " " });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Venue");
    }

    // Kiểm lỗi đầu vào của trường Venue.
    [Fact]
    public void InvalidVenueCase7IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { Venue = new string('a', 301) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "Venue");
    }

    // Kiểm lỗi đầu vào của trường RegistrationCloseAt.
    [Fact]
    public void InvalidRegistrationCloseAtCase8IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { RegistrationCloseAt = default });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "RegistrationCloseAt");
    }

    // Kiểm lỗi đầu vào của trường StartDate.
    [Fact]
    public void InvalidStartDateCase9IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { StartDate = default });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "StartDate");
    }

    // Kiểm lỗi đầu vào của trường EndDate.
    [Fact]
    public void InvalidEndDateCase10IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndDate = new DateOnly(2026, 12, 2) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndDate" && failure.ErrorCode == "EndDateBeforeStart");
    }

    // Kiểm lỗi đầu vào của trường EndDate.
    [Fact]
    public void InvalidEndDateCase11IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { EndDate = new DateOnly(2026, 12, 17) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "EndDate");
    }

    // Kiểm lỗi đầu vào của trường DayStartTime.
    [Fact]
    public void InvalidDayStartTimeCase12IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DayStartTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DayStartTime");
    }

    // Kiểm lỗi đầu vào của trường DayEndTime.
    [Fact]
    public void InvalidDayEndTimeCase13IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DayEndTime = new TimeOnly(8, 10) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DayEndTime");
    }

    // Kiểm lỗi đầu vào của trường DayEndTime.
    [Fact]
    public void InvalidDayEndTimeCase14IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DayEndTime = new TimeOnly(7, 0) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DayEndTime");
    }

    // Kiểm lỗi đầu vào của trường DayEndTime.
    [Fact]
    public void InvalidDayEndTimeCase15IsRejected()
    {
        var validationResult = _validator.Validate(Valid() with { DayEndTime = new TimeOnly(8, 45) });
        Assert.Contains(validationResult.Errors, failure => failure.PropertyName == "DayEndTime");
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

    // BR17: Báo lỗi đúng trường mô tả thiếu bản dịch, kể cả chuỗi toàn khoảng trắng.
    [Theory]
    [InlineData("Mô tả", null, "DescriptionEn")]
    [InlineData("Mô tả", "", "DescriptionEn")]
    [InlineData("Mô tả", " \t ", "DescriptionEn")]
    [InlineData(null, "Description", "DescriptionVi")]
    [InlineData("", "Description", "DescriptionVi")]
    [InlineData(" \t ", "Description", "DescriptionVi")]
    public void MissingDescriptionTranslationIsRejected(string? descriptionVi, string? descriptionEn, string missingProperty)
    {
        var command = Valid() with { DescriptionVi = descriptionVi, DescriptionEn = descriptionEn };
        var validationResult = _validator.Validate(command);

        var failure = Assert.Single(validationResult.Errors);
        Assert.Equal(missingProperty, failure.PropertyName);
        Assert.Equal("TranslationRequired", failure.ErrorCode);
    }

    // BR17: Mô tả có thể cùng trống hoặc cùng có nội dung; tên tính độ dài sau khi trim.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" \t ", " ")]
    [InlineData(null, " ")]
    [InlineData(" ", null)]
    [InlineData(" Mô tả ", " Description ")]
    public void CompleteOrEmptyDescriptionPairPasses(string? descriptionVi, string? descriptionEn)
    {
        var command = Valid() with
        {
            NameEn = " " + new string('a', 200) + " ",
            DescriptionVi = descriptionVi,
            DescriptionEn = descriptionEn
        };

        Assert.True(_validator.Validate(command).IsValid);
    }

    // Helper của các test validation (cùng lớp): Dữ liệu hợp lệ làm mốc.
    private static CreateConferenceCommand Valid()
    {
        return new CreateConferenceCommand("Tên", "Conference", null, null, "Hà Nội", new DateOnly(2026, 12, 3), new DateOnly(2026, 12, 4), new TimeOnly(8, 0), new TimeOnly(17, 0), new DateTimeOffset(2026, 12, 1, 23, 59, 0, TimeSpan.FromHours(7)), true);
    }
}
