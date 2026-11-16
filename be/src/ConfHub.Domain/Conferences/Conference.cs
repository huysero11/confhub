using ConfHub.Domain.Common;

namespace ConfHub.Domain.Conferences;

public sealed class Conference : BaseEntity, IAggregateRoot
{
    public const int MaxDays = 14;
    private readonly List<ConferenceDay> _days = [];

    private Conference()
    {
    }

    public Guid OwnerId { get; private set; }
    public string NameVi { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string? DescriptionVi { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public DateTime RegistrationCloseAt { get; private set; }
    public bool RequireApproval { get; private set; }
    public ConferenceStatus Status { get; private set; }
    public IReadOnlyCollection<ConferenceDay> Days => _days.AsReadOnly();

    public static Conference Create(
        Guid ownerId,
        ConferenceDetails details,
        DateOnly startDate,
        DateOnly endDate,
        TimeRange defaultHours,
        DateOnly today,
        DateTime nowUtc,
        TimeZoneInfo venueTimeZone)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("End date must not precede start date.", nameof(endDate));
        }

        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;
        if (dayCount > MaxDays)
        {
            throw new DomainException(ConferenceErrorCodes.TooManyDays, "A conference can have at most 14 days.");
        }

        EnsureDateNotInPast(startDate, today);
        EnsureFutureDeadline(details.RegistrationCloseAt, nowUtc);

        var conference = new Conference
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Status = ConferenceStatus.Draft
        };
        conference.ApplyDetails(details);

        for (var i = 0; i < dayCount; i++)
        {
            conference._days.Add(new ConferenceDay(conference.Id, startDate.AddDays(i), defaultHours));
        }

        conference.EnsureDeadlineBeforeStart(details.RegistrationCloseAt, venueTimeZone);

        return conference;
    }

    public void UpdateDetails(ConferenceDetails details, DateTime nowUtc, TimeZoneInfo venueTimeZone)
    {
        if (details.RegistrationCloseAt != RegistrationCloseAt)
        {
            EnsureFutureDeadline(details.RegistrationCloseAt, nowUtc);
        }

        EnsureDeadlineBeforeStart(details.RegistrationCloseAt, venueTimeZone);
        ApplyDetails(details);
    }

    public bool IsOwnedBy(Guid userId)
    {
        return OwnerId == userId;
    }

    // ---------- NGÀY TỔ CHỨC ----------
    public ConferenceDay? FindDay(Guid dayId)
    {
        return _days.Find(day => day.Id == dayId);
    }

    public ConferenceDay AddDay(DateOnly date, TimeRange hours, DateOnly today, TimeZoneInfo venueTimeZone)
    {
        EnsureDateNotInPast(date, today);
        EnsureDateAvailable(date, null);

        if (_days.Count >= MaxDays)
        {
            throw new DomainException(ConferenceErrorCodes.TooManyDays, "A conference can have at most 14 days.");
        }

        EnsureDeadlineBeforeStart(RegistrationCloseAt, venueTimeZone, null, date, hours);

        var day = new ConferenceDay(Id, date, hours);
        _days.Add(day);

        return day;
    }

    public void UpdateDay(Guid dayId, DateOnly date, TimeRange hours, DateOnly today, TimeZoneInfo venueTimeZone)
    {
        var day = FindDay(dayId) ?? throw new InvalidOperationException("Day must exist in the conference.");
        if (day.Date != date)
        {
            EnsureDateNotInPast(date, today);
            EnsureDateAvailable(date, dayId);
        }

        foreach (var breakSlot in day.Breaks)
        {
            if (!hours.Contains(breakSlot.Time))
            {
                throw new DomainException(ConferenceErrorCodes.BreakOutsideDay, "Day hours must contain every break.");
            }
        }

        EnsureDeadlineBeforeStart(RegistrationCloseAt, venueTimeZone, dayId, date, hours);
        day.Update(date, hours);
    }

    public void RemoveDay(Guid dayId)
    {
        var day = FindDay(dayId) ?? throw new InvalidOperationException("Day must exist in the conference.");
        if (_days.Count == 1)
        {
            throw new DomainException(ConferenceErrorCodes.LastDayRequired, "At least one conference day is required.");
        }

        _days.Remove(day);
    }

    // ---------- KHUNG NGHỈ ----------
    public BreakSlot AddBreak(Guid dayId, string name, TimeRange time)
    {
        var day = FindDay(dayId) ?? throw new InvalidOperationException("Day must exist in the conference.");
        EnsureBreakFits(day, time, null);

        return day.AddBreak(name, time);
    }

    public void UpdateBreak(Guid dayId, Guid breakId, string name, TimeRange time)
    {
        var day = FindDay(dayId) ?? throw new InvalidOperationException("Day must exist in the conference.");
        var breakSlot = day.FindBreak(breakId) ?? throw new InvalidOperationException("Break must exist in the day.");
        EnsureBreakFits(day, time, breakId);
        breakSlot.Update(name, time);
    }

    public void RemoveBreak(Guid dayId, Guid breakId)
    {
        var day = FindDay(dayId) ?? throw new InvalidOperationException("Day must exist in the conference.");
        var breakSlot = day.FindBreak(breakId) ?? throw new InvalidOperationException("Break must exist in the day.");
        day.RemoveBreak(breakSlot);
    }

    // ---------- KIỂM BẤT BIẾN ----------
    private static void EnsureBreakFits(ConferenceDay day, TimeRange time, Guid? excludedBreakId)
    {
        if (!day.Hours.Contains(time))
        {
            throw new DomainException(ConferenceErrorCodes.BreakOutsideDay, "Break must be inside the day hours.");
        }

        foreach (var breakSlot in day.Breaks)
        {
            if (breakSlot.Id != excludedBreakId && breakSlot.Time.Overlaps(time))
            {
                throw new DomainException(ConferenceErrorCodes.BreakOverlap, "Breaks must not overlap.");
            }
        }
    }

    private static void EnsureDateNotInPast(DateOnly date, DateOnly today)
    {
        if (date < today)
        {
            throw new DomainException(ConferenceErrorCodes.DayInPast, "New conference dates cannot be in the past.");
        }
    }

    // Helper của Create, UpdateDetails (cùng lớp): Hạn đăng ký vừa đặt phải ở tương lai.
    private static void EnsureFutureDeadline(DateTime deadlineUtc, DateTime nowUtc)
    {
        if (deadlineUtc <= nowUtc)
        {
            throw new DomainException(ConferenceErrorCodes.RegistrationCloseInPast, "Registration deadline must be in the future.");
        }
    }

    private static string? TrimOrNull(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private void ApplyDetails(ConferenceDetails details)
    {
        NameVi = details.NameVi.Trim();
        NameEn = details.NameEn.Trim();
        DescriptionVi = TrimOrNull(details.DescriptionVi);
        DescriptionEn = TrimOrNull(details.DescriptionEn);
        Venue = details.Venue.Trim();
        RegistrationCloseAt = details.RegistrationCloseAt;
        RequireApproval = details.RequireApproval;
    }

    /*
        Hạn đăng ký phải trước hoặc đúng giờ bắt đầu của ngày sớm nhất.

        Khi đổi lịch thì sẽ có những ngày bị thay thế hoặc di chuyển, nên có những ngày đề xuất mới.
        Cần kiểm tra phép đổi hợp lệ không với các ngày đề xuất đó
    */
    private void EnsureDeadlineBeforeStart(
        DateTime deadlineUtc,
        TimeZoneInfo venueTimeZone,
        Guid? replacedDayId = null,
        DateOnly? proposedDate = null,
        TimeRange? proposedHours = null)
    {
        var firstStart = DateTime.MaxValue;
        foreach (var day in _days)
        {
            if (day.Id == replacedDayId)
            {
                continue;
            }

            var localStart = day.Date.ToDateTime(day.Hours.Start);
            if (localStart < firstStart)
            {
                firstStart = localStart;
            }
        }

        if (proposedDate.HasValue && proposedHours is not null)
        {
            var proposedStart = proposedDate.Value.ToDateTime(proposedHours.Start);
            if (proposedStart < firstStart)
            {
                firstStart = proposedStart;
            }
        }

        var firstStartUtc = TimeZoneInfo.ConvertTimeToUtc(firstStart, venueTimeZone);
        if (deadlineUtc > firstStartUtc)
        {
            throw new DomainException(ConferenceErrorCodes.RegistrationCloseAfterStart, "Registration must close by the first day start.");
        }
    }

    private void EnsureDateAvailable(DateOnly date, Guid? excludeDayId)
    {
        foreach (var day in _days)
        {
            if (day.Id != excludeDayId && day.Date == date)
            {
                throw new DomainException(ConferenceErrorCodes.DayDateTaken, "The conference already has this date.");
            }
        }
    }
}
