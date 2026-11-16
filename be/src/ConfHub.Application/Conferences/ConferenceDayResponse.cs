using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences;

public sealed record ConferenceDayResponse(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    List<BreakSlotResponse> Breaks)
{
    public static ConferenceDayResponse FromConferenceDay(ConferenceDay day)
    {
        var breaks = new List<BreakSlotResponse>();
        foreach (var breakSlot in day.Breaks.OrderBy(breakSlot => breakSlot.Time.Start))
        {
            breaks.Add(BreakSlotResponse.FromBreakSlot(breakSlot));
        }

        return new ConferenceDayResponse(day.Id, day.Date, day.Hours.Start, day.Hours.End, breaks);
    }
}
