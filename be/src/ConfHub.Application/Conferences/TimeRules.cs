using ConfHub.Domain.Common;
using FluentValidation;

namespace ConfHub.Application.Conferences;

public static class TimeRules
{
    public const int MinimumDayMinutes = 60;

    public static IRuleBuilderOptions<T, TimeOnly> QuarterHour<T>(this IRuleBuilder<T, TimeOnly> ruleBuilder)
    {
        return ruleBuilder
            .Must(TimeRange.IsQuarterHour)
            .WithErrorCode("QuarterHourStep")
            .WithMessage("Time must be a multiple of 15 minutes.");
    }

    public static IRuleBuilderOptions<T, TimeOnly> EndAfterStart<T>(
        this IRuleBuilder<T, TimeOnly> ruleBuilder,
        Func<T, TimeOnly> getStart)
    {
        return ruleBuilder
            .Must((command, endTime) => endTime > getStart(command))
            .WithErrorCode("EndNotAfterStart")
            .WithMessage("End time must be after start time.");
    }

    public static IRuleBuilderOptions<T, TimeOnly> ConferenceDayEnd<T>(
        this IRuleBuilder<T, TimeOnly> ruleBuilder,
        Func<T, TimeOnly> getStart)
    {
        return ruleBuilder
            .QuarterHour()
            .EndAfterStart(getStart)
            .Must((command, endTime) => IsLongEnough(getStart(command), endTime))
            .WithErrorCode("DayTooShort")
            .WithMessage("A conference day must last at least 60 minutes.");
    }

    // Helper của ConferenceDayEnd (cùng lớp): Chỉ báo DayTooShort khi kết thúc đã sau bắt đầu
    // (ngược lại đã có lỗi EndNotAfterStart, không báo trùng hai lỗi).
    private static bool IsLongEnough(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
        {
            return true;
        }

        return (end - start).TotalMinutes >= MinimumDayMinutes;
    }
}
