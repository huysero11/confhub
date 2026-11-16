using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Tests.Conferences;

public class ConferenceTests
{
    // ---------- TẠO VÀ SỬA THÔNG TIN ----------

    // Tạo đủ ngày, trạng thái và chuẩn hóa các chuỗi theo đặc tả.
    [Fact]
    public void CreateBuildsDraftDaysAndTrimsDetails()
    {
        var conference = ConferenceTestData.Create();
        Assert.Equal(ConferenceStatus.Draft, conference.Status);
        Assert.Equal("Công nghệ Xanh 2026", conference.NameVi);
        Assert.Equal("Green Technology 2026", conference.NameEn);
        Assert.Equal("Mô tả", conference.DescriptionVi);
        Assert.Equal("Description", conference.DescriptionEn);
        Assert.Equal("Hà Nội", conference.Venue);
        Assert.True(conference.RequireApproval);
        Assert.Equal([ConferenceTestData.FirstDate, ConferenceTestData.FirstDate.AddDays(1)], conference.Days.Select(day => day.Date));
        Assert.All(conference.Days, day => Assert.Equal(ConferenceTestData.Hours, day.Hours));
        Assert.All(conference.Days, day => Assert.Equal(conference.Id, day.ConferenceId));
    }

    // BR17: Mô tả cùng bỏ trống được chuẩn hóa về null khi tạo và sửa hội nghị.
    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    [InlineData(" \t ", "")]
    public void EmptyDescriptionsAreStoredAsNull(string? descriptionVi, string? descriptionEn)
    {
        var details = ConferenceTestData.Details with { DescriptionVi = descriptionVi, DescriptionEn = descriptionEn };
        var conference = CreateWithDetails(details);

        Assert.Null(conference.DescriptionVi);
        Assert.Null(conference.DescriptionEn);

        conference.UpdateDetails(ConferenceTestData.Details, ConferenceTestData.NowUtc, ConferenceTestData.TimeZone);
        Assert.Equal("Description", conference.DescriptionEn);

        conference.UpdateDetails(details with { NameEn = " Updated conference " }, ConferenceTestData.NowUtc, ConferenceTestData.TimeZone);
        Assert.Equal("Updated conference", conference.NameEn);
        Assert.Null(conference.DescriptionVi);
        Assert.Null(conference.DescriptionEn);
    }

    // Aggregate vẫn chặn quá số ngày khi không đi qua validator.
    [Fact]
    public void CreateRejectsFifteenDays()
    {
        AssertCode(() => ConferenceTestData.Create(15), ConferenceErrorCodes.TooManyDays);
    }

    // Ngày đầu trước ngày hiện tại theo giờ nơi tổ chức bị chặn.
    [Fact]
    public void CreateRejectsPastDay()
    {
        AssertCode(
            () => Conference.Create(
                Guid.NewGuid(),
                ConferenceTestData.Details,
                ConferenceTestData.Today.AddDays(-1),
                ConferenceTestData.Today,
                ConferenceTestData.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.NowUtc,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.DayInPast);
    }

    // Hạn đúng giờ đầu tiên ở Việt Nam hợp lệ; sau một phút bị chặn.
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void DeadlineUsesVenueTimeZone(int extraMinutes, bool expectedValid)
    {
        var details = ConferenceTestData.Details with
        {
            RegistrationCloseAt = new DateTime(2026, 12, 3, 1, extraMinutes, 0, DateTimeKind.Utc)
        };
        if (expectedValid)
        {
            Assert.Equal(details.RegistrationCloseAt, CreateWithDetails(details).RegistrationCloseAt);
        }
        else
        {
            AssertCode(() => CreateWithDetails(details), ConferenceErrorCodes.RegistrationCloseAfterStart);
        }
    }

    // Hạn bằng hiện tại cũng không còn là tương lai.
    [Fact]
    public void CreateRejectsPastOrCurrentDeadline()
    {
        AssertCode(
            () => CreateWithDetails(
                ConferenceTestData.Details with
                {
                    RegistrationCloseAt = ConferenceTestData.NowUtc
                }),
            ConferenceErrorCodes.RegistrationCloseInPast);
    }

    // Sửa mô tả không buộc đổi hạn đã qua; đổi hạn về quá khứ bị chặn.
    [Fact]
    public void UpdateDetailsMayKeepExpiredDeadlineButCannotSetAnotherExpiredDeadline()
    {
        var conference = ConferenceTestData.Create();
        var now = new DateTime(2026, 12, 2, 3, 0, 0, DateTimeKind.Utc);
        conference.UpdateDetails(ConferenceTestData.Details with { NameVi = "Đổi tên" }, now, ConferenceTestData.TimeZone);
        Assert.Equal("Đổi tên", conference.NameVi);
        AssertCode(
            () => conference.UpdateDetails(
                ConferenceTestData.Details with
                {
                    RegistrationCloseAt = now.AddMinutes(-1)
                },
                now,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.RegistrationCloseInPast);
    }

    // ---------- NGÀY ----------

    // Thêm ngày kiểm trùng, số lượng và hạn trước khi sửa collection.
    [Fact]
    public void AddDayRejectsDuplicateLimitAndEarlierStart()
    {
        var conference = ConferenceTestData.Create();
        AssertCode(
            () => conference.AddDay(
                ConferenceTestData.FirstDate,
                ConferenceTestData.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.DayDateTaken);
        AssertCode(
            () => conference.AddDay(
                new DateOnly(2026, 12, 1),
                ConferenceTestData.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.RegistrationCloseAfterStart);
        Assert.Equal(2, conference.Days.Count);
        var fullConference = ConferenceTestData.Create(14);
        AssertCode(
            () => fullConference.AddDay(
                ConferenceTestData.FirstDate.AddDays(14),
                ConferenceTestData.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.TooManyDays);
    }

    // Ngày không đổi có thể ở quá khứ; ngày mới không được ở quá khứ hoặc trùng ngày khác.
    [Fact]
    public void UpdateDayValidatesOnlyChangedDateAndLeavesStateOnFailure()
    {
        var conference = ConferenceTestData.Create();
        var day = conference.Days.First();
        var tomorrow = ConferenceTestData.FirstDate.AddDays(1);
        conference.UpdateDay(day.Id, day.Date, day.Hours, tomorrow, ConferenceTestData.TimeZone);
        AssertCode(
            () => conference.UpdateDay(
                day.Id,
                day.Date.AddDays(-1),
                day.Hours,
                tomorrow,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.DayInPast);
        AssertCode(
            () => conference.UpdateDay(
                day.Id,
                tomorrow,
                day.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.DayDateTaken);
        AssertCode(
            () => conference.UpdateDay(
                day.Id,
                new DateOnly(2026, 12, 1),
                day.Hours,
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.RegistrationCloseAfterStart);
        Assert.Equal(ConferenceTestData.FirstDate, day.Date);
    }

    // Không thể xóa ngày cuối; ngày có khung nghỉ được gỡ trọn khỏi aggregate.
    [Fact]
    public void RemoveDayKeepsAtLeastOneDay()
    {
        var conference = ConferenceTestData.Create();
        var first = conference.Days.First();
        conference.AddBreak(first.Id, "Nghỉ", TimeRange.Create(new TimeOnly(10, 0), new TimeOnly(10, 15)));
        conference.RemoveDay(first.Id);
        Assert.Null(conference.FindDay(first.Id));
        AssertCode(() => conference.RemoveDay(conference.Days.Single().Id), ConferenceErrorCodes.LastDayRequired);
    }

    // ---------- KHUNG NGHỈ ----------

    // Khung nghỉ nằm ngoài ngày hoặc chồng khung khác bị chặn; chạm đầu mút được phép.
    [Fact]
    public void BreaksMustFitAndMayTouchButNotOverlap()
    {
        var conference = ConferenceTestData.Create();
        var day = conference.Days.First();
        var lunch = conference.AddBreak(day.Id, " Nghỉ trưa ", TimeRange.Create(new TimeOnly(11, 30), new TimeOnly(13, 0)));
        Assert.Equal("Nghỉ trưa", lunch.Name);
        AssertCode(() => conference.AddBreak(day.Id, "Ngoài giờ", TimeRange.Create(new TimeOnly(7, 0), new TimeOnly(8, 15))), ConferenceErrorCodes.BreakOutsideDay);
        AssertCode(() => conference.AddBreak(day.Id, "Chồng", TimeRange.Create(new TimeOnly(11, 0), new TimeOnly(11, 45))), ConferenceErrorCodes.BreakOverlap);
        var adjacent = conference.AddBreak(day.Id, "Liền kề", TimeRange.Create(new TimeOnly(13, 0), new TimeOnly(13, 15)));
        conference.UpdateBreak(day.Id, lunch.Id, "Trưa", lunch.Time);
        Assert.Equal("Trưa", lunch.Name);
        AssertCode(() => conference.UpdateBreak(day.Id, adjacent.Id, "Chồng", lunch.Time), ConferenceErrorCodes.BreakOverlap);
        AssertCode(
            () => conference.UpdateDay(
                day.Id,
                day.Date,
                TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(12, 0)),
                ConferenceTestData.Today,
                ConferenceTestData.TimeZone),
            ConferenceErrorCodes.BreakOutsideDay);
        Assert.Equal(ConferenceTestData.Hours, day.Hours);
        conference.RemoveBreak(day.Id, adjacent.Id);
        Assert.Null(day.FindBreak(adjacent.Id));
    }

    // Id lạ là lỗi lập trình ở Domain; handler phải đổi thành 404 trước khi gọi.
    [Fact]
    public void MissingChildIdsAreProgrammingErrors()
    {
        var conference = ConferenceTestData.Create();
        Assert.Throws<InvalidOperationException>(() => conference.RemoveDay(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => conference.RemoveBreak(conference.Days.First().Id, Guid.NewGuid()));
    }

    // ---------- HELPER ----------

    // Helper của DeadlineUsesVenueTimeZone, CreateRejectsPastOrCurrentDeadline (cùng lớp): Tạo với hạn cần thử.
    private static Conference CreateWithDetails(ConferenceDetails details)
    {
        return Conference.Create(
            Guid.NewGuid(),
            details,
            ConferenceTestData.FirstDate,
            ConferenceTestData.FirstDate,
            ConferenceTestData.Hours,
            ConferenceTestData.Today,
            ConferenceTestData.NowUtc,
            ConferenceTestData.TimeZone);
    }

    // Helper của các test bất biến (cùng lớp): So sánh mã lỗi nghiệp vụ.
    private static void AssertCode(Action action, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(action).Code);
    }
}
