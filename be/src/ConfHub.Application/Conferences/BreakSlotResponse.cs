using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences;

public sealed record BreakSlotResponse(Guid Id, string Name, TimeOnly StartTime, TimeOnly EndTime)
{
    public static BreakSlotResponse FromBreakSlot(BreakSlot breakSlot)
    {
        return new BreakSlotResponse(breakSlot.Id, breakSlot.Name, breakSlot.Time.Start, breakSlot.Time.End);
    }
}
